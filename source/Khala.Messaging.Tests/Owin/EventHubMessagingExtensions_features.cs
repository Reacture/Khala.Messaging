namespace Khala.Owin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeBlogEngine;
    using FluentAssertions;
    using global::Owin;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.Owin.Testing;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;

    [TestClass]
    public class EventHubMessagingExtensions_features
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagingextensions-eventhub-connectionstring";
        public const string EventHubPathPropertyName = "eventhubmessagingextensions-eventhub-path";
        public const string StorageConnectionStringPropertyName = "eventhubmessagingextensions-storage-connectionstring";
        public const string ConsumerGroupPropertyName = "eventhubmessagingextensions-eventhub-consumergroup";

        private static string ConnectionParametersRequired => $@"
EventProcessorHost connection information is not set. To run tests on the EventHubMessagingExtensions class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{EventHubPathPropertyName}"" value=""your event hub path for testing"" />
    <Parameter name=""{StorageConnectionStringPropertyName}"" value=""your storage connection string for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim();

        private IFixture fixture;
        private string eventHubConnectionString;
        private string eventHubPath;
        private string storageConnectionString;
        private string consumerGroupName;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture();

            eventHubConnectionString = (string)TestContext.Properties[EventHubConnectionStringPropertyName];
            eventHubPath = (string)TestContext.Properties[EventHubPathPropertyName];
            storageConnectionString = (string)TestContext.Properties[StorageConnectionStringPropertyName];
            consumerGroupName = (string)TestContext.Properties[ConsumerGroupPropertyName] ?? EventHubConsumerGroup.DefaultGroupName;

            if (string.IsNullOrWhiteSpace(eventHubConnectionString) ||
                string.IsNullOrWhiteSpace(eventHubPath) ||
                string.IsNullOrWhiteSpace(storageConnectionString))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }
        }

        [TestMethod]
        public async Task UseEventMessageProcessor_registers_EventMessageProcessorFactory_correctly()
        {
            // Arrange
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            Envelope handled = null;
            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(It.IsNotNull<Envelope>(), It.IsAny<CancellationToken>()))
                .Callback<Envelope, CancellationToken>((m, t) => handled = m)
                .Returns(Task.FromResult(true));

            var serializer = new EventDataSerializer();
            var eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString, eventHubPath);
            var messageBus = new EventHubMessageBus(eventHubClient, serializer);

            CancellationToken cancellationToken;
            using (TestServer server = TestServer.Create(app =>
            {
                app.UseEventMessageProcessor(
                    new EventProcessorHost(
                        eventHubPath,
                        consumerGroupName,
                        eventHubConnectionString,
                        storageConnectionString),
                    serializer,
                    messageHandler);
                var properties = new AppProperties(app.Properties);
                cancellationToken = properties.OnAppDisposing;
            }))
            {
                // Act
                await messageBus.Send(envelope, CancellationToken.None);
                for (int i = 0; i < 10 && handled == null; i++)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(1000));
                }

                // Assert
                Mock.Get(messageHandler).Verify(x => x.Handle(It.IsAny<Envelope>(), cancellationToken), Times.Once());
                handled.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
        }
    }
}
