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
        private static EventHubClient eventHubClient;
        private static string consumerGroupName;
        private IFixture fixture;
        private IMessageSerializer serializer;
        private EventHubMessageBus sut;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var connectionString = (string)context.Properties["eventhubmessagebus-eventhub-connectionstring"];
            var path = (string)context.Properties["eventhubmessagebus-eventhub-path"];
            if (string.IsNullOrWhiteSpace(connectionString) == false &&
                string.IsNullOrWhiteSpace(path) == false)
            {
                eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, path);
                consumerGroupName =
                    (string)context.Properties["eventhubmessagebus-eventhub-consumergroup"] ??
                    EventHubConsumerGroup.DefaultGroupName;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            if (eventHubClient == null)
            {
                Assert.Inconclusive(@"
Event Hub 연결 정보가 설정되지 않았습니다. EventHubMessageBus 클래스에 대한 테스트를 실행하려면 *.runsettings 파일에 다음과 같이 Event Hub 연결 정보를 설정합니다.

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""eventhubmessagebus-eventhub-connectionstring"" value=""your event hub connection string for testing"" />
    <Parameter name=""eventhubmessagebus-eventhub-path"" value=""your event hub path for testing"" />
    <Parameter name=""eventhubmessagebus-eventhub-consumergroup"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
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

            EventHubConsumerGroup consumerGroup =
                eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo =
                await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers =
                await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.Send(message, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(1);
                string partition = null;
                EventData eventData = null;
                foreach (EventHubReceiver receiver in receivers)
                {
                    eventData = await receiver.ReceiveAsync(waitTime);
                    if (eventData != null)
                    {
                        partition = receiver.PartitionId;
                        break;
                    }
                }

                eventData.Should().NotBeNull();
                byte[] bytes = eventData.GetBytes();
                string value = Encoding.UTF8.GetString(bytes);

                TestContext.WriteLine("Partition: {0}, Value: {1}", partition, value);

                object actual = serializer.Deserialize(value);
                actual.Should().BeOfType<FooMessage>();
                actual.ShouldBeEquivalentTo(message);
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

            EventHubConsumerGroup consumerGroup =
                eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo =
                await eventHubClient.GetRuntimeInformationAsync();
            List<EventHubReceiver> receivers =
                await GetReceivers(consumerGroup, runtimeInfo);

            try
            {
                // Act
                await sut.Send(message, CancellationToken.None);

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
            List<FooMessage> messages = fixture
                .Build<FooMessage>()
                .With(x => x.SourceId, sourceId)
                .CreateMany()
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
                await sut.SendBatch(messages, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(3);
                string partition = null;
                var eventDataList = new List<EventData>();
                foreach (EventHubReceiver receiver in receivers)
                {
                    IEnumerable<EventData> eventData = await
                        receiver.ReceiveAsync(messages.Count, waitTime);
                    if (eventData?.Any() ?? false)
                    {
                        eventDataList.AddRange(eventData);
                        partition = receiver.PartitionId;
                        break;
                    }
                }
                TestContext.WriteLine("Partition: {0}", partition);
                var actual = new List<object>(eventDataList.Select(x => Deserialize(x)));
                actual.Should().OnlyContain(x => x is FooMessage);
                actual.ShouldAllBeEquivalentTo(messages);
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
            List<FooMessage> messages = fixture
                .Build<FooMessage>()
                .With(x => x.SourceId, sourceId)
                .CreateMany()
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
                await sut.SendBatch(messages, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(3);
                var eventDataList = new List<EventData>();
                foreach (EventHubReceiver receiver in receivers)
                {
                    IEnumerable<EventData> eventData = await
                        receiver.ReceiveAsync(messages.Count, waitTime);
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
