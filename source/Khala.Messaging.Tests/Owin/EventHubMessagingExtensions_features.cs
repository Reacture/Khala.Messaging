﻿namespace Khala.Owin
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeBlogEngine;
    using FluentAssertions;
    using global::Owin;
    using Khala.Messaging;
    using Khala.Messaging.Azure;
    using Khala.TransientFaultHandling;
    using Microsoft.Azure.EventHubs;
    using Microsoft.Azure.EventHubs.Processor;
    using Microsoft.Owin.BuilderProperties;
    using Microsoft.Owin.Testing;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class EventHubMessagingExtensions_features
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagingextensions-eventhub-connectionstring";
        public const string StorageConnectionStringPropertyName = "eventhubmessagingextensions-storage-connectionstring";
        public const string ConsumerGroupPropertyName = "eventhubmessagingextensions-eventhub-consumergroup";
        public const string LeaseContainerNamePropertyName = "eventhubmessagingextensions-lease-container-name";

        private static string ConnectionParametersRequired => $@"
EventProcessorHost connection information is not set. To run tests on the EventHubMessagingExtensions class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
    <Parameter name=""{StorageConnectionStringPropertyName}"" value=""your storage connection string for testing"" />
    <Parameter name=""{LeaseContainerNamePropertyName}"" value=""your lease container name for testing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim();

        private IFixture fixture;
        private string eventHubConnectionString;
        private string consumerGroupName;
        private string storageConnectionString;
        private string leaseContainerName;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture();

            eventHubConnectionString = (string)TestContext.Properties[EventHubConnectionStringPropertyName];
            consumerGroupName = (string)TestContext.Properties[ConsumerGroupPropertyName] ?? PartitionReceiver.DefaultConsumerGroupName;
            storageConnectionString = (string)TestContext.Properties[StorageConnectionStringPropertyName];
            leaseContainerName = (string)TestContext.Properties[LeaseContainerNamePropertyName];

            if (string.IsNullOrWhiteSpace(eventHubConnectionString) ||
                string.IsNullOrWhiteSpace(leaseContainerName) ||
                string.IsNullOrWhiteSpace(storageConnectionString) ||
                string.IsNullOrWhiteSpace(leaseContainerName))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            var eventProcessorHost = new EventProcessorHost(
                leaseContainerName,
                consumerGroupName,
                eventHubConnectionString,
                storageConnectionString,
                leaseContainerName);
            fixture.Inject(eventProcessorHost);
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(EventHubMessagingExtensions));
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
            var eventHubClient = EventHubClient.CreateFromConnectionString(eventHubConnectionString);
            var messageBus = new EventHubMessageBus(eventHubClient, serializer);

            CancellationToken cancellationToken;
            using (TestServer server = TestServer.Create(app =>
            {
                app.UseEventMessageProcessor(
                    new EventProcessorHost(
                        leaseContainerName,
                        consumerGroupName,
                        eventHubConnectionString,
                        storageConnectionString,
                        leaseContainerName),
                    serializer,
                    messageHandler);
                var properties = new AppProperties(app.Properties);
                cancellationToken = properties.OnAppDisposing;
            }))
            {
                // Act
                await messageBus.Send(envelope, CancellationToken.None);
                await RetryPolicy<Envelope>
                    .LinearTransientDefault(5, TimeSpan.FromMilliseconds(100))
                    .Run(ct => Task.FromResult(handled), cancellationToken);
            }

            // Assert
            Mock.Get(messageHandler).Verify(x => x.Handle(It.IsAny<Envelope>(), cancellationToken));
            handled.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
