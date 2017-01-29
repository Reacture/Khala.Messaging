using System;
using System.Threading;
using System.Threading.Tasks;
using Arcane.Messaging;
using Arcane.Messaging.Azure;
using FluentAssertions;
using Microsoft.Owin.BuilderProperties;
using Microsoft.Owin.Testing;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Owin;
using Ploeh.AutoFixture;

namespace Arcane.Owin
{
    [TestClass]
    public class EventHubMessagingExtensions_features
    {
        public const string EventHubConnectionStringPropertyName = "eventhubmessagingextensions-eventhub-connectionstring";
        public const string EventHubPathPropertyName = "eventhubmessagingextensions-eventhub-path";
        public const string StorageConnectionStringPropertyName = "eventhubmessagingextensions-storage-connectionstring";
        public const string ConsumerGroupPropertyName = "eventhubmessagingextensions-eventhub-consumergroup";

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
            consumerGroupName =
                (string)TestContext.Properties[ConsumerGroupPropertyName] ??
                EventHubConsumerGroup.DefaultGroupName;

            if (string.IsNullOrWhiteSpace(eventHubConnectionString) ||
                string.IsNullOrWhiteSpace(eventHubPath) ||
                string.IsNullOrWhiteSpace(storageConnectionString))
            {
                Assert.Inconclusive($@"
EventProcessorHost 연결 정보가 설정되지 않았습니다. ReactiveMessagingExtensions 클래스에 대한 테스트를 실행하려면 *.runsettings 파일에 다음과 같이 연결 정보를 설정합니다.

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{EventHubConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{EventHubPathPropertyName}"" value=""your event hub path for testing"" />
    <Parameter name=""{StorageConnectionStringPropertyName}"" value=""your storage connection string for testing"" />
    <Parameter name=""{ConsumerGroupPropertyName}"" value=""[OPTIONAL] your event hub consumer group name for testing"" />
  </TestRunParameters>  
</RunSettings>

참고문서
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim());
            }
        }

        public class FooMessage
        {
            public Guid Prop1 { get; set; }

            public int Prop2 { get; set; }

            public double Prop3 { get; set; }

            public string Prop4 { get; set; }
        }

        [TestMethod]
        public async Task UseEventMessageProcessor_registers_EventMessageProcessorFactory_correctly()
        {
            // Arrange
            var message = fixture.Create<FooMessage>();
            var envelope = new Envelope(message);

            Envelope handled = null;
            var messageHandler = Mock.Of<IMessageHandler>();
            Mock.Get(messageHandler)
                .Setup(x => x.Handle(It.IsNotNull<Envelope>(), It.IsAny<CancellationToken>()))
                .Callback<Envelope, CancellationToken>((m, t) => handled = m)
                .Returns(Task.FromResult(true));

            var messageSerializer = new JsonMessageSerializer();

            var messageBus = new EventHubMessageBus(
                EventHubClient.CreateFromConnectionString(eventHubConnectionString, eventHubPath),
                messageSerializer);

            var eventProcessorHost = new EventProcessorHost(
                eventHubPath,
                consumerGroupName,
                eventHubConnectionString,
                storageConnectionString);

            CancellationToken cancellationToken;
            using (TestServer server = TestServer.Create(app =>
            {
                app.UseEventMessageProcessor(
                    eventProcessorHost,
                    messageHandler,
                    messageSerializer,
                    Mock.Of<IMessageProcessingExceptionHandler<EventData>>());
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
                Mock.Get(messageHandler).Verify(
                    x => x.Handle(It.IsAny<Envelope>(), cancellationToken),
                    Times.Once());
                handled.Should().NotBeNull();
                handled.Message.Should().BeOfType<FooMessage>();
                handled.ShouldBeEquivalentTo(envelope);
            }
        }
    }
}
