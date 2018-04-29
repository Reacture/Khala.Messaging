namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ServiceBusMessageBus_specs
    {
        public const string ConnectionStringParam = "ServiceBusMessageBus/ConnectionString";
        public const string EntityPathParam = "ServiceBusMessageBus/EntityPath";

        private static readonly string ConnectionParametersRequired = $@"Service Bus connection information is not set. To run tests on the ServiceBusMessageBus class, you must set the connection information in the *.runsettings file as follows:

<?xml version=""1.0"" encoding=""utf-8"" ?>
<RunSettings>
  <TestRunParameters>
    <Parameter name=""{ConnectionStringParam}"" value=""your connection string to the Service Bus"" />
    <Parameter name=""{EntityPathParam}"" value=""[OPTIONAL] The name of the queue"" />
  </TestRunParameters>  
</RunSettings>

References
- https://msdn.microsoft.com/en-us/library/jj635153.aspx";

        private static ServiceBusConnectionStringBuilder _connectionStringBuilder;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            if (context.Properties.TryGetValue(ConnectionStringParam, out object connectionString) &&
                context.Properties.TryGetValue(EntityPathParam, out object entityPath))
            {
                _connectionStringBuilder = new ServiceBusConnectionStringBuilder((string)connectionString)
                {
                    EntityPath = (string)entityPath,
                };
            }
            else
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }
        }

        public TestContext TestContext { get; set; }

        private static async Task ReceiveAndForgetAll()
        {
            var receiver = new MessageReceiver(_connectionStringBuilder, ReceiveMode.ReceiveAndDelete);

            while (await receiver.ReceiveAsync(TimeSpan.FromMilliseconds(1000)) != null)
            {
            }

            await receiver.CloseAsync();
        }

#pragma warning disable SA1009 // Disable warning SA1009(Closing parenthesis must be spaced correctly) for generic types of tuples

        private static async Task<(Message received, DateTime receivedAt)> ReceiveSingle()
        {
            IEnumerable<(Message, DateTime)> result = await Receive(maxMessageCount: 1);
            return result.Single();
        }

        private static async Task<IEnumerable<(Message received, DateTime receivedAt)>> Receive(int maxMessageCount)
        {
            var receiver = new MessageReceiver(_connectionStringBuilder, ReceiveMode.ReceiveAndDelete);
            var result = new List<(Message, DateTime)>();

            while (result.Count < maxMessageCount)
            {
                Message received = await receiver.ReceiveAsync();
                if (received == null)
                {
                    break;
                }

                result.Add((received, DateTime.UtcNow));
            }

            await receiver.CloseAsync();

            return result;
        }

#pragma warning restore SA1009 // Disable warning SA1009(Closing parenthesis must be spaced correctly) for generic types of tuples

        [TestMethod]
        public void sut_implements_IScheduledMessageBus()
        {
            typeof(ServiceBusMessageBus).Should().Implement<IScheduledMessageBus>();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            builder.Customize(new AutoMoqCustomization());
            builder.Inject(_connectionStringBuilder);
            new GuardClauseAssertion(builder).Verify(typeof(ServiceBusMessageBus));
        }

        [TestMethod]
        public async Task Send_sends_scheduled_brokered_message_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, serializer);

            var scheduled = new ScheduledEnvelope(
                new Envelope(new Fixture().Create<SomeMessage>()),
                DateTimeOffset.Now.Add(TimeSpan.FromMilliseconds(10000)));

            // Act
            await sut.Send(scheduled, CancellationToken.None);

            // Assert
            (Message received, DateTime receivedAt) = await ReceiveSingle();
            var precision = TimeSpan.FromMilliseconds(1000);
            receivedAt.Should().BeOnOrAfter(scheduled.ScheduledTime.UtcDateTime.AddTicks(-precision.Ticks));

            received.MessageId.Should().Be(scheduled.Envelope.MessageId.ToString("n"));
            received.CorrelationId.Should().Be(null);

            object message = serializer.Deserialize(Encoding.UTF8.GetString(received.Body));
            message.Should().BeEquivalentTo(scheduled.Envelope.Message);
        }

        [TestMethod]
        public async Task Send_works_for_already_passed_scheduled_time()
        {
            // Arrange
            await ReceiveAndForgetAll();

            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, serializer);

            var scheduled = new ScheduledEnvelope(
                new Envelope(new Fixture().Create<SomeMessage>()),
                DateTimeOffset.Now.AddTicks(-TimeSpan.FromDays(1).Ticks));

            // Act
            await sut.Send(scheduled, CancellationToken.None);

            // Assert
            (Message received, DateTime receivedAt) = await ReceiveSingle();
            var precision = TimeSpan.FromMilliseconds(1000);
            receivedAt.Should().BeOnOrAfter(scheduled.ScheduledTime.UtcDateTime.AddTicks(-precision.Ticks));

            object message = serializer.Deserialize(Encoding.UTF8.GetString(received.Body));
            message.Should().BeEquivalentTo(scheduled.Envelope.Message);
        }

        [TestMethod]
        public async Task Send_sets_OperationId_correctly_if_exists()
        {
            // Arrange
            await ReceiveAndForgetAll();

            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, serializer);

            string operationId = Guid.NewGuid().ToString();
            var scheduled = new ScheduledEnvelope(
                new Envelope(
                    messageId: Guid.NewGuid(),
                    message: new Fixture().Create<SomeMessage>(),
                    operationId: operationId),
                DateTimeOffset.Now);

            // Act
            await sut.Send(scheduled, CancellationToken.None);

            // Assert
            (Message received, DateTime receivedAt) = await ReceiveSingle();
            received.UserProperties.Should().Contain("Khala.Messaging.Envelope.OperationId", operationId);
        }

        [TestMethod]
        public async Task Send_sets_CorrelationId_correctly_if_exists()
        {
            // Arrange
            await ReceiveAndForgetAll();

            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, serializer);

            var correlationId = Guid.NewGuid();
            var scheduled = new ScheduledEnvelope(
                new Envelope(
                    messageId: Guid.NewGuid(),
                    message: new Fixture().Create<SomeMessage>(),
                    correlationId: correlationId),
                DateTimeOffset.Now);

            // Act
            await sut.Send(scheduled, CancellationToken.None);

            // Assert
            (Message received, DateTime receivedAt) = await ReceiveSingle();
            received.CorrelationId.Should().Be(correlationId.ToString("n"));
        }

        [TestMethod]
        public async Task Send_sets_Contributor_user_property_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            IMessageSerializer serializer = new JsonMessageSerializer();
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, serializer);

            var messageId = Guid.NewGuid();
            string contributor = Guid.NewGuid().ToString();
            var scheduled = new ScheduledEnvelope(
                new Envelope(
                    messageId,
                    message: new Fixture().Create<SomeMessage>(),
                    contributor: contributor),
                DateTimeOffset.Now);

            // Act
            await sut.Send(scheduled, CancellationToken.None);

            // Assert
            (Message received, DateTime receivedAt) = await ReceiveSingle();
            received.UserProperties.Should().Contain("Khala.Messaging.Envelope.Contributor", contributor);
        }

        [TestMethod]
        public async Task Close_releases_message_sender_resources()
        {
            var sut = new ServiceBusMessageBus(_connectionStringBuilder, new JsonMessageSerializer());

            await sut.Close();

            var scheduled = new ScheduledEnvelope(new Envelope(new object()), DateTimeOffset.Now);
            Func<Task> action = () => sut.Send(scheduled, CancellationToken.None);
            action.Should().Throw<Exception>();
        }

        public class SomeMessage
        {
            public string Content { get; set; }
        }
    }
}
