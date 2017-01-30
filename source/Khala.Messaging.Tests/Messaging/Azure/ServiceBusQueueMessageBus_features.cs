using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.Idioms;

namespace Khala.Messaging.Azure
{
    [TestClass]
    public class ServiceBusQueueMessageBus_features
    {
        public const string ConnectionStringPropertyName = "servicebusqueuemessagebus-connectionstring";
        public const string QueueNamePropertyName = "servicebusqueuemessagebus-path";

        private static string connectionString;
        private static string queueName;
        private static QueueClient queueClient;
        private IFixture fixture;
        private IMessageSerializer serializer;
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
                Assert.Inconclusive($@"
Service Bus Queue 연결 정보가 설정되지 않았습니다. ServiceBusQueueMessageBus 클래스에 대한 테스트를 실행하려면 *.runsettings 파일에 다음과 같이 Service Bus Queue 연결 정보를 설정합니다.

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{ConnectionStringPropertyName}"" value=""your event hub connection string for testing"" />
    <Parameter name=""{QueueNamePropertyName}"" value=""your event hub path for testing"" />
  </TestRunParameters>  
</RunSettings>

참고문서
- https://msdn.microsoft.com/en-us/library/jj635153.aspx
".Trim());
            }

            fixture = new Fixture();
            serializer = new JsonMessageSerializer();

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

        public class FooMessage : IPartitioned
        {
            public string SourceId { get; set; }

            public int Value { get; set; }

            string IPartitioned.PartitionKey => SourceId;
        }

        [TestMethod]
        public void SendBatch_has_guard_clause_for_null_message()
        {
            var envelopes = new[]
            {
                fixture.Create<Envelope>(),
                null,
                fixture.Create<Envelope>()
            };

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
                var message = fixture.Create<FooMessage>();
                var envelope = new Envelope(message);

                // Act
                await sut.Send(envelope, CancellationToken.None);

                // Assert
                received = await queueClient.ReceiveAsync(TimeSpan.FromSeconds(3));
                received.Should().NotBeNull();
                using (var stream = received.GetBody<Stream>())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string value = await reader.ReadToEndAsync();
                    object actual = serializer.Deserialize(value);
                    actual.Should().BeOfType<Envelope>();
                    actual.As<Envelope>().Message.Should().BeOfType<FooMessage>();
                    actual.ShouldBeEquivalentTo(envelope);
                }
            }
            finally
            {
                // Cleanup
                received?.Complete();
            }
        }

        [TestMethod]
        public async Task Send_sets_PartitionKey_correctly()
        {
            var message = fixture.Create<FooMessage>();
            var envelope = new Envelope(message);

            await sut.Send(envelope, CancellationToken.None);

            BrokeredMessage received = await queueClient.ReceiveAsync(TimeSpan.FromSeconds(3));
            received.PartitionKey.Should().Be(message.SourceId);
            await received.CompleteAsync();
        }

        [TestMethod]
        public async Task Send_sets_MessageId_correctly()
        {
            var message = fixture.Create<FooMessage>();
            var envelope = new Envelope(message);

            await sut.Send(envelope, CancellationToken.None);

            BrokeredMessage received = await queueClient.ReceiveAsync(TimeSpan.FromSeconds(3));
            try
            {
                received.MessageId.Should().Be($"{envelope.MessageId:n}");
            }
            finally
            {
                received?.Complete();
            }
        }

        [TestMethod]
        public async Task Send_sets_CorrelationId_correctly()
        {
            var message = fixture.Create<FooMessage>();
            var correlationId = Guid.NewGuid();
            var envelope = new Envelope(correlationId, message);

            await sut.Send(envelope, CancellationToken.None);

            BrokeredMessage received = await queueClient.ReceiveAsync(TimeSpan.FromSeconds(3));
            try
            {
                received.CorrelationId.Should().Be($"{correlationId:n}");
            }
            finally
            {
                received?.Complete();
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

            // Act
            await sut.SendBatch(envelopes, CancellationToken.None);

            // Assert
            var received = new List<BrokeredMessage>(
                await queueClient.ReceiveBatchAsync(envelopes.Count, TimeSpan.FromSeconds(10)) ??
                Enumerable.Empty<BrokeredMessage>());

            try
            {
                received.Should().HaveCount(envelopes.Count);
                List<object> actual = received
                    .Select(Deserialize)
                    .ToList();
                actual.Should().OnlyContain(x => x is Envelope);
                actual.Cast<Envelope>().Should().OnlyContain(x => x.Message is FooMessage);
                actual.ShouldAllBeEquivalentTo(envelopes);
            }
            finally
            {
                // Cleanup
                CompleteAll(received);
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

            // Act
            await sut.SendBatch(envelopes, CancellationToken.None);

            // Assert
            var received = new List<BrokeredMessage>(
                await queueClient.ReceiveBatchAsync(envelopes.Count, TimeSpan.FromSeconds(10)) ??
                Enumerable.Empty<BrokeredMessage>());

            try
            {
                received.Should().OnlyContain(x => x.PartitionKey == sourceId);
            }
            finally
            {
                // Cleanup
                CompleteAll(received);
            }
        }

        private object Deserialize(BrokeredMessage message)
        {
            using (var stream = message.GetBody<Stream>())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string value = reader.ReadToEnd();
                return serializer.Deserialize(value);
            }
        }
    }
}
