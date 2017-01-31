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

namespace Khala.Messaging.Azure
{
    [TestClass]
    public class ServiceBusQueueMessageBus_features
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
        private IFixture fixture;
        private BrokeredMessageSerializer serializer;
        private ServiceBusQueueMessageBus sut;

        public TestContext TestContext { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            connectionString = (string)context.Properties[ConnectionStringPropertyName];
            queueName = (string)context.Properties[QueueNamePropertyName];
            if (string.IsNullOrWhiteSpace(connectionString) == false &&
                string.IsNullOrWhiteSpace(queueName) == false)
            {
                queueClient = QueueClient.CreateFromConnectionString(connectionString, queueName);
                ClearQueue(queueClient);
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            queueClient?.Close();
        }

        private static void ClearQueue(QueueClient queueClient)
        {
            while (queueClient.Peek() != null)
            {
                CompleteAll(queueClient.ReceiveBatch(100));
            }
        }

        private static void CompleteAll(IEnumerable<BrokeredMessage> messages)
        {
            Task[] tasks = messages
                .Select(m => Task.Factory.StartNew(() => m.Complete()))
                .ToArray();
            Task.WaitAll(tasks);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            if (queueClient == null)
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            fixture = new Fixture().Customize(new AutoMoqCustomization());
            serializer = new BrokeredMessageSerializer();

            fixture.Inject(queueClient);
            fixture.Inject(serializer);

            sut = new ServiceBusQueueMessageBus(queueClient, serializer);
        }

        [TestMethod]
        public void class_has_guard_clauses()
        {
            var assertion = new GuardClauseAssertion(fixture);
            assertion.Verify(typeof(ServiceBusQueueMessageBus));
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
            BrokeredMessage received = null;
            try
            {
                var message = fixture.Create<BlogPostCreated>();
                var envelope = new Envelope(message);

                // Act
                await sut.Send(envelope, CancellationToken.None);

                // Assert
                received = await queueClient.ReceiveAsync(TimeSpan.FromSeconds(3));
                received.Should().NotBeNull();
                Envelope actual = await serializer.Deserialize(received);
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                // Cleanup
                received?.Complete();
            }
        }
    }
}
