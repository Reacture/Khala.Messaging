namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using FakeBlogEngine;
    using FluentAssertions;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class ServiceBusMessageBus_specs
    {
        public const string ConnectionStringPropertyName = "servicebusqueuemessagebus-connectionstring";
        public const string QueueNamePropertyName = "servicebusqueuemessagebus-path";

        private static string ConnectionParametersRequired => $@"
Service Bus Queue connection information is not set. To run tests on the ServiceBusQueueMessageBus class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{ConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{QueueNamePropertyName}"" value=""your event hub path for testing"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim();

        private static string connectionString;
        private static string queueName;
        private static QueueClient queueClient;
        private static ConcurrentQueue<Message> receivedQueue;
        private IFixture fixture;
        private ServiceBusMessageSerializer serializer;
        private ServiceBusMessageBus sut;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext context)
        {
            connectionString = (string)context.Properties[ConnectionStringPropertyName];
            queueName = (string)context.Properties[QueueNamePropertyName];
            if (string.IsNullOrWhiteSpace(connectionString) == false &&
                string.IsNullOrWhiteSpace(queueName) == false)
            {
                queueClient = new QueueClient(
                    connectionString,
                    queueName,
                    receiveMode: ReceiveMode.ReceiveAndDelete);

                receivedQueue = new ConcurrentQueue<Message>();

                queueClient.RegisterMessageHandler(
                    (message, cancellationToken) =>
                    {
                        receivedQueue.Enqueue(message);
                        return Task.CompletedTask;
                    },
                    exceptionReceivedEventArgs => Task.CompletedTask);

                await ClearQueue(queueClient);
            }
        }

        private static async Task ClearQueue(QueueClient queueClient)
        {
            do
            {
                await Task.Delay(1000);
            }
            while (receivedQueue.TryDequeue(out Message received));
        }

        [ClassCleanup]
        public static async Task ClassCleanup() => await queueClient?.CloseAsync();

        [TestInitialize]
        public void TestInitialize()
        {
            if (queueClient == null)
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            fixture = new Fixture().Customize(new AutoMoqCustomization());
            serializer = new ServiceBusMessageSerializer();

            fixture.Inject(queueClient);
            fixture.Inject(serializer);

            sut = new ServiceBusMessageBus(queueClient, serializer);
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(ServiceBusMessageBus));
        }

        [TestMethod]
        public void SendBatch_has_guard_clause_for_null_message()
        {
            var envelopes = new Envelope[] { null };
            Action action = () => sut.SendBatch(envelopes, CancellationToken.None);
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "envelopes");
        }

        [TestMethod]
        public async Task Send_sends_message_correctly()
        {
            // Arrange
            var message = fixture.Create<BlogPostCreated>();
            var envelope = new Envelope(message);

            // Act
            await sut.Send(envelope, CancellationToken.None);

            // Assert
            await Task.Delay(TimeSpan.FromSeconds(3));
            receivedQueue.Should().ContainSingle();
            receivedQueue.TryDequeue(out Message received);
            received.Should().NotBeNull();
            Envelope actual = await serializer.Deserialize(received);
            actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
        }
    }
}
