using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeBlogEngine;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoMoq;
using Ploeh.AutoFixture.Idioms;
using static Microsoft.ServiceBus.Messaging.EventHubConsumerGroup;

namespace Khala.Messaging.Azure
{
    [TestClass]
    public class EventHubMessageBus_specs
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagebus-eventhub-connectionstring";
        public const string EventHubPathPropertyName = "eventhubmessagebus-eventhub-path";
        public const string ConsumerGroupPropertyName = "eventhubmessagebus-eventhub-consumergroup";

        private static string ConnectionParametersRequired => $@"
Event Hub connection information is not set. To run tests on the EventHubMessageBus class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{EventHubPathPropertyName}"" value=""your event hub path for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim();

        private static EventHubClient eventHubClient;
        private static string consumerGroupName;
        private IFixture fixture;
        private EventDataSerializer serializer;
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
                consumerGroupName = (string)context.Properties[ConsumerGroupPropertyName] ?? DefaultGroupName;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            if (eventHubClient == null)
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            fixture = new Fixture().Customize(new AutoMoqCustomization());
            fixture.Inject(eventHubClient);
            serializer = new EventDataSerializer();
            sut = new EventHubMessageBus(eventHubClient, serializer);
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(EventHubMessageBus));
        }

        [TestMethod]
        public void SendBatch_has_guard_clause_for_null_envelope()
        {
            var envelopes = new Envelope[] { null };
            Func<Task> action = () => sut.SendBatch(envelopes);
            action.ShouldThrow<ArgumentException>()
                .Where(x => x.ParamName == "envelopes");
        }

        [TestMethod]
        public async Task Send_sends_message_correctly()
        {
            // Arrange
            var message = fixture.Create<BlogPostCreated>();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);

            List<EventHubReceiver> receivers = await GetReceivers();

            try
            {
                // Act
                await sut.Send(envelope, CancellationToken.None);

                // Assert
                var waitTime = TimeSpan.FromSeconds(1);
                Task<EventData>[] tasks = receivers.Select(r => r.ReceiveAsync(waitTime)).ToArray();
                await Task.WhenAll(tasks);
                EventData eventData = tasks.Select(t => t.Result).FirstOrDefault(r => r != null);

                eventData.Should().NotBeNull();
                Envelope actual = await serializer.Deserialize(eventData);
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                // Cleanup
                receivers.ForEach(r => r.Close());
            }
        }

        private async Task<List<EventHubReceiver>> GetReceivers()
        {
            EventHubConsumerGroup consumerGroup = eventHubClient.GetConsumerGroup(consumerGroupName);
            EventHubRuntimeInformation runtimeInfo = await eventHubClient.GetRuntimeInformationAsync();
            var receivers = new List<EventHubReceiver>();
            foreach (string partition in runtimeInfo.PartitionIds)
            {
                receivers.Add(await consumerGroup.CreateReceiverAsync(partition, EndOfStream));
            }
            return receivers;
        }
    }
}
