using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;

namespace ReactiveArchitecture.Messaging.Azure
{
    [TestClass]
    public class EventHubMessageBus_features
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagebus-eventhub-connectionstring";
        public const string EventHubPathPropertyName = "eventhubmessagebus-eventhub-path";
        public const string ConsumerGroupPropertyName = "eventhubmessagebus-eventhub-consumergroup";

        private static EventHubClient eventHubClient;
        private static string consumerGroupName;
        private IFixture fixture;
        private IMessageSerializer serializer;
        private EventHubMessageBus sut;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var connectionString = (string)context.Properties[EventHubConnectionStringPropertyName];
            var path = (string)context.Properties[EventHubPathPropertyName];
            if (string.IsNullOrWhiteSpace(connectionString) == false &&
                string.IsNullOrWhiteSpace(path) == false)
            {
                eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, path);
                consumerGroupName =
                    (string)context.Properties[ConsumerGroupPropertyName] ??
                    EventHubConsumerGroup.DefaultGroupName;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            if (eventHubClient == null)
            {
                Assert.Inconclusive($@"
Event Hub 연결 정보가 설정되지 않았습니다. EventHubMessageBus 클래스에 대한 테스트를 실행하려면 *.runsettings 파일에 다음과 같이 Event Hub 연결 정보를 설정합니다.

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{EventHubPathPropertyName}"" value=""your event hub path for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
  </TestRunParameters>  
</RunSettings>

참고문서
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim());
            }

            fixture = new Fixture();
            serializer = new JsonMessageSerializer();
            sut = new EventHubMessageBus(eventHubClient, serializer);
        }

        public class FooMessage : IPartitioned
        {
            public string SourceId { get; set; }

            public int Value { get; set; }

            string IPartitioned.PartitionKey => SourceId;
        }

        [TestMethod]
        public async Task Send_sends_message_correctly()
        {
            // Arrange
            var message = fixture.Create<FooMessage>();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);

            EventHubConsumerGroup consumerGroup = eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo = await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers = await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.Send(envelope, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(1);
                EventData eventData = null;
                foreach (EventHubReceiver receiver in receivers)
                {
                    eventData = await receiver.ReceiveAsync(waitTime);
                    if (eventData != null)
                    {
                        break;
                    }
                }

                eventData.Should().NotBeNull();
                byte[] bytes = eventData.GetBytes();
                string value = Encoding.UTF8.GetString(bytes);
                object actual = serializer.Deserialize(value);
                actual.Should().BeOfType<Envelope>();
                actual.As<Envelope>().Message.Should().BeOfType<FooMessage>();
                actual.ShouldBeEquivalentTo(envelope);
            }
            finally
            {
                // Cleanup
                receivers.ForEach(r => r.Close());
            }
        }

        [TestMethod]
        public async Task Send_sets_partition_key_correctly()
        {
            // Arrange
            var message = fixture.Create<FooMessage>();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);

            EventHubConsumerGroup consumerGroup = eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo = await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers = await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.Send(envelope, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(1);
                EventData eventData = null;
                foreach (EventHubReceiver receiver in receivers)
                {
                    eventData = await receiver.ReceiveAsync(waitTime);
                    if (eventData != null)
                    {
                        break;
                    }
                }

                eventData.PartitionKey.Should().Be(message.SourceId);
            }
            finally
            {
                // Cleanup
                receivers.ForEach(r => r.Close());
            }
        }

        [TestMethod]
        public async Task SendBatch_sends_messages_correctly()
        {
            // Arrange
            var sourceId = fixture.Create<string>();
            List<Envelope> envelopes = fixture
                .Build<FooMessage>()
                .With(x => x.SourceId, sourceId)
                .CreateMany()
                .Select(m => new Envelope(m))
                .ToList();

            EventHubConsumerGroup consumerGroup = eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo = await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers = await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.SendBatch(envelopes, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(3);
                var eventDataList = new List<EventData>();
                foreach (EventHubReceiver receiver in receivers)
                {
                    IEnumerable<EventData> eventData = await
                        receiver.ReceiveAsync(envelopes.Count, waitTime);
                    if (eventData?.Any() ?? false)
                    {
                        eventDataList.AddRange(eventData);
                        break;
                    }
                }
                var actual = new List<object>(eventDataList.Select(x => Deserialize(x)));
                actual.Should().OnlyContain(x => x is Envelope);
                actual.Cast<Envelope>().Should().OnlyContain(e => e.Message is FooMessage);
                actual.ShouldAllBeEquivalentTo(envelopes);
            }
            finally
            {
                // Cleanup
                receivers.ForEach(r => r.Close());
            }
        }

        [TestMethod]
        public async Task SendBatch_sets_partition_keys_correctly()
        {
            // Arrange
            var sourceId = fixture.Create<string>();
            List<Envelope> envelopes = fixture
                .Build<FooMessage>()
                .With(x => x.SourceId, sourceId)
                .CreateMany()
                .Select(m => new Envelope(m))
                .ToList();

            EventHubConsumerGroup consumerGroup =
                eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo =
                await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers =
                await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.SendBatch(envelopes, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(3);
                var eventDataList = new List<EventData>();
                foreach (EventHubReceiver receiver in receivers)
                {
                    IEnumerable<EventData> eventData = await
                        receiver.ReceiveAsync(envelopes.Count, waitTime);
                    if (eventData?.Any() ?? false)
                    {
                        eventDataList.AddRange(eventData);
                        break;
                    }
                }
                eventDataList.Should().OnlyContain(x => x.PartitionKey == sourceId);
            }
            finally
            {
                // Cleanup
                receivers.ForEach(r => r.Close());
            }
        }

        private object Deserialize(EventData eventData)
        {
            byte[] bytes = eventData.GetBytes();
            string value = Encoding.UTF8.GetString(bytes);
            return serializer.Deserialize(value);
        }

        private async Task<List<EventHubReceiver>> GetReceivers(
            EventHubConsumerGroup consumerGroup,
            EventHubRuntimeInformation runtimeInfo)
        {
            var receivers = new List<EventHubReceiver>();
            foreach (string partition in runtimeInfo.PartitionIds)
            {
                EventHubReceiver receiver = await
                    consumerGroup.CreateReceiverAsync(
                        partition,
                        EventHubConsumerGroup.EndOfStream);
                receivers.Add(receiver);
            }

            return receivers;
        }
    }
}
