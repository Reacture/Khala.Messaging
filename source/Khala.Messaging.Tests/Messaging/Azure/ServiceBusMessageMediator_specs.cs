namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Azure.ServiceBus;
    using Microsoft.Azure.ServiceBus.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class ServiceBusMessageMediator_specs
    {
        public const string ConnectionStringParam = "ServiceBusMessageMediator/ConnectionString";
        public const string EntityPathParam = "ServiceBusMessageMediator/EntityPath";

        private static readonly string ConnectionParametersRequired = $@"Service Bus connection information is not set. To run tests on the ServiceBusMessageMediator class, you must set the connection information in the *.runsettings file as follows:

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
            string connectionString = (string)context.Properties[ConnectionStringParam];
            string entityPath = (string)context.Properties[EntityPathParam];

            if (string.IsNullOrWhiteSpace(connectionString) ||
                string.IsNullOrWhiteSpace(entityPath))
            {
                Assert.Inconclusive(ConnectionParametersRequired);
            }

            _connectionStringBuilder = new ServiceBusConnectionStringBuilder(connectionString)
            {
                EntityPath = entityPath
            };
        }

        public TestContext TestContext { get; set; }

        private static async Task ReceiveAndForgetAll()
        {
            var receiver = new MessageReceiver(_connectionStringBuilder, ReceiveMode.ReceiveAndDelete);

            while (await receiver.ReceiveAsync(10, TimeSpan.FromMilliseconds(1000)) != null)
            {
            }

            await receiver.CloseAsync();
        }

        private static async Task SendMessage(Envelope envelope, JsonMessageSerializer serializer)
        {
            var messageBus = new ServiceBusMessageBus(_connectionStringBuilder, serializer);
            await messageBus.Send(new ScheduledEnvelope(envelope, DateTimeOffset.Now.AddMinutes(-1.0)), default);
            await messageBus.Close();
        }

        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            builder.Customize(new AutoMoqCustomization());
            builder.Inject(_connectionStringBuilder);
            new GuardClauseAssertion(builder).Verify(typeof(ServiceBusMessageMediator));
        }

        [TestMethod]
        public async Task message_handler_sends_message_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            var messageBus = new MessageBus();
            var serializer = new JsonMessageSerializer();

            var fixture = new Fixture();

            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: Guid.NewGuid(),
                correlationId: Guid.NewGuid(),
                contributor: fixture.Create<string>(),
                message: fixture.Create<SomeMessage>());

            // Act
            Func<Task> closeFunction = ServiceBusMessageMediator.Start(_connectionStringBuilder, messageBus, serializer);
            try
            {
                await SendMessage(envelope, serializer);

                // Assert
                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(3000)), messageBus.SentMessage);
                messageBus.SentMessage.Status.Should().Be(TaskStatus.RanToCompletion);
                Envelope actual = await messageBus.SentMessage;
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                await closeFunction.Invoke();
            }
        }

        [TestMethod]
        public async Task message_handler_sends_message_not_having_operationId_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            var messageBus = new MessageBus();
            var serializer = new JsonMessageSerializer();

            var fixture = new Fixture();

            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: default,
                correlationId: Guid.NewGuid(),
                contributor: fixture.Create<string>(),
                message: fixture.Create<SomeMessage>());

            // Act
            Func<Task> closeFunction = ServiceBusMessageMediator.Start(_connectionStringBuilder, messageBus, serializer);
            try
            {
                await SendMessage(envelope, serializer);

                // Assert
                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(3000)), messageBus.SentMessage);
                messageBus.SentMessage.Status.Should().Be(TaskStatus.RanToCompletion);
                Envelope actual = await messageBus.SentMessage;
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                await closeFunction.Invoke();
            }
        }

        [TestMethod]
        public async Task message_handler_sends_message_not_having_correlationId_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            var messageBus = new MessageBus();
            var serializer = new JsonMessageSerializer();

            var fixture = new Fixture();

            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: Guid.NewGuid(),
                correlationId: default,
                contributor: fixture.Create<string>(),
                message: fixture.Create<SomeMessage>());

            // Act
            Func<Task> closeFunction = ServiceBusMessageMediator.Start(_connectionStringBuilder, messageBus, serializer);
            try
            {
                await SendMessage(envelope, serializer);

                // Assert
                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(3000)), messageBus.SentMessage);
                messageBus.SentMessage.Status.Should().Be(TaskStatus.RanToCompletion);
                Envelope actual = await messageBus.SentMessage;
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                await closeFunction.Invoke();
            }
        }

        [TestMethod]
        public async Task message_handler_sends_message_not_having_contributor_correctly()
        {
            // Arrange
            await ReceiveAndForgetAll();

            var messageBus = new MessageBus();
            var serializer = new JsonMessageSerializer();

            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: Guid.NewGuid(),
                correlationId: Guid.NewGuid(),
                contributor: default,
                message: new Fixture().Create<SomeMessage>());

            // Act
            Func<Task> closeFunction = ServiceBusMessageMediator.Start(_connectionStringBuilder, messageBus, serializer);
            try
            {
                await SendMessage(envelope, serializer);

                // Assert
                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(3000)), messageBus.SentMessage);
                messageBus.SentMessage.Status.Should().Be(TaskStatus.RanToCompletion);
                Envelope actual = await messageBus.SentMessage;
                actual.ShouldBeEquivalentTo(envelope, opts => opts.RespectingRuntimeTypes());
            }
            finally
            {
                await closeFunction.Invoke();
            }
        }

        [TestMethod]
        public async Task Start_returns_close_function()
        {
            // Arrange
            await ReceiveAndForgetAll();

            var messageBus = new MessageBus();
            var serializer = new JsonMessageSerializer();

            var envelope = new Envelope(
                messageId: Guid.NewGuid(),
                operationId: default,
                correlationId: default,
                contributor: default,
                message: new Fixture().Create<SomeMessage>());

            // Act
            Func<Task> closeFunction = ServiceBusMessageMediator.Start(_connectionStringBuilder, messageBus, serializer);
            try
            {
                await closeFunction.Invoke();
                await SendMessage(envelope, serializer);

                // Assert
                await Task.WhenAny(Task.Delay(TimeSpan.FromMilliseconds(3000)), messageBus.SentMessage);
                messageBus.SentMessage.Status.Should().NotBe(TaskStatus.RanToCompletion);
            }
            finally
            {
                await closeFunction.Invoke();
            }
        }

        public class SomeMessage
        {
            public string Content { get; set; }
        }

        public class MessageBus : IMessageBus
        {
            private readonly TaskCompletionSource<Envelope> _completion = new TaskCompletionSource<Envelope>();

            public Task<Envelope> SentMessage => _completion.Task;

            public Task Send(Envelope envelope, CancellationToken cancellationToken)
            {
                _completion.SetResult(envelope);
                return Task.CompletedTask;
            }

            public Task Send(IEnumerable<Envelope> envelopes, CancellationToken cancellationToken) => throw new NotSupportedException();
        }
    }
}
