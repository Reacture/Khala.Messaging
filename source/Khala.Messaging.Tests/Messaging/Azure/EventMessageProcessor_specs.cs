namespace Khala.Messaging.Azure
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.ServiceBus.Messaging;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.AutoMoq;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class EventMessageProcessor_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture().Customize(new AutoMoqCustomization());
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(EventMessageProcessor));
        }

        [TestMethod]
        public void ProcessEventAsync_has_guard_clause_against_null_message()
        {
            // Arrange
            var sut = new EventMessageProcessor(
                new MessageProcessorCore<EventData>(
                    Mock.Of<IMessageHandler>(),
                    Mock.Of<IMessageDataSerializer<EventData>>(),
                    Mock.Of<IMessageProcessingExceptionHandler<EventData>>()),
                CancellationToken.None);

            var random = new Random();
            var messages = Enumerable
                .Range(0, 10)
                .Select(_ => new EventData())
                .Concat(new[] { default(EventData) })
                .OrderBy(_ => random.Next());

            // Act
            Func<Task> action = () => sut.ProcessEventsAsync(new PartitionContext(), messages);

            // Assert
            action.ShouldThrow<ArgumentException>().Where(x => x.ParamName == "messages");
        }
    }
}
