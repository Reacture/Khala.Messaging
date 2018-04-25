namespace Khala.Messaging.Azure
{
    using System;
    using AutoFixture;
    using AutoFixture.Idioms;
    using FluentAssertions;
    using Microsoft.Azure.EventHubs;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class EventDataExtensions_specs
    {
        [TestMethod]
        public void sut_has_guard_clauses()
        {
            var builder = new Fixture();
            new GuardClauseAssertion(builder).Verify(typeof(EventDataExtensions));
        }

        [TestMethod]
        public void GetOperationId_returns_operation_id_correctly()
        {
            var operationId = Guid.NewGuid();
            var eventData = new EventData(new byte[] { })
            {
                Properties = { ["OperationId"] = operationId },
            };

            Guid? actual = eventData.GetOperationId();

            actual.Should().Be(operationId);
        }

        [TestMethod]
        public void GetOperationId_returns_null_if_value_is_not_GUID()
        {
            var operationId = Guid.NewGuid();
            var eventData = new EventData(new byte[] { })
            {
                Properties = { ["OperationId"] = "foo" },
            };

            Guid? actual = eventData.GetOperationId();

            actual.Should().NotHaveValue();
        }

        [TestMethod]
        public void GetOperationId_returns_null_if_property_not_exists()
        {
            var operationId = Guid.NewGuid();
            var eventData = new EventData(new byte[] { });

            Guid? actual = eventData.GetOperationId();

            actual.Should().NotHaveValue();
        }
    }
}
