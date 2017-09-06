namespace Khala.Messaging
{
    using System;
    using System.Reflection;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Ploeh.AutoFixture;
    using Ploeh.AutoFixture.Idioms;

    [TestClass]
    public class JsonMessageSerializer_specs
    {
        private IFixture fixture;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            fixture = new Fixture();
        }

        public class MutableMessage
        {
            public Guid GuidProp { get; set; }

            public int Int32Prop { get; set; }

            public double DoubleProp { get; set; }

            public string StringProp { get; set; }
        }

        public class ImmutableMessage
        {
            public ImmutableMessage(
                Guid guidProp,
                int int32Prop,
                double doubleProp,
                string stringProp)
            {
                GuidProp = guidProp;
                Int32Prop = int32Prop;
                DoubleProp = doubleProp;
                StringProp = stringProp;
            }

            public Guid GuidProp { get; }

            public int Int32Prop { get; }

            public double DoubleProp { get; }

            public string StringProp { get; }
        }

        public class MessageWithDateTimeOffsetProperty
        {
            public DateTimeOffset DateTimeOffsetProp { get; set; }
        }

        [TestMethod]
        public void sut_implements_IMessageSerializer()
        {
            var sut = new JsonMessageSerializer();
            sut.Should().BeAssignableTo<IMessageSerializer>();
        }

        [TestMethod]
        public void constructor_has_guard_clause()
        {
            var builder = new Fixture { OmitAutoProperties = true };
            var assertion = new GuardClauseAssertion(builder);
            assertion.Verify(typeof(JsonMessageSerializer).GetConstructors());
        }

        [TestMethod]
        public void Deserialize_has_guard_clause()
        {
            var assertion = new GuardClauseAssertion(fixture);
            Type type = typeof(JsonMessageSerializer);
            MethodInfo method = type.GetMethod(
                nameof(JsonMessageSerializer.Deserialize));
            assertion.Verify(method);
        }

        [TestMethod]
        public void Deserialize_restores_mutable_message_correctly()
        {
            // Arrange
            var sut = new JsonMessageSerializer();
            var message = fixture.Create<MutableMessage>();
            string serialized = sut.Serialize(message);
            TestContext.WriteLine(serialized);

            // Act
            object actual = sut.Deserialize(serialized);

            // Assert
            actual.Should().BeOfType<MutableMessage>();
            actual.ShouldBeEquivalentTo(message);
        }

        [TestMethod]
        public void Deserialize_restores_immutable_message_correctly()
        {
            // Arrange
            var sut = new JsonMessageSerializer();
            var message = fixture.Create<ImmutableMessage>();
            string serialized = sut.Serialize(message);
            TestContext.WriteLine(serialized);

            // Act
            object actual = sut.Deserialize(serialized);

            // Assert
            actual.Should().BeOfType<ImmutableMessage>();
            actual.ShouldBeEquivalentTo(message);
        }

        [TestMethod]
        public void Deserialize_restores_message_of_unknown_type_to_dynamic()
        {
            // Arrange
            string prop = $"{Guid.NewGuid()}";
            var json = $"{{ \"$type\": \"UnknownNamespace.UnknownMessage, UnknownAssembly\", \"Prop\": \"{prop}\" }}";
            var sut = new JsonMessageSerializer();
            var actual = default(object);

            // Act
            Action action = () => actual = sut.Deserialize(json);

            // Assert
            action.ShouldNotThrow();
            actual.Should().NotBeNull();
            ((string)((dynamic)actual).Prop).Should().Be(prop);
        }

        [TestMethod]
        public void Deserialize_restores_untyped_message_to_dynamic()
        {
            // Arrange
            string prop = $"{Guid.NewGuid()}";
            var json = $"{{ \"Prop\": \"{prop}\" }}";
            var sut = new JsonMessageSerializer();
            var actual = default(object);

            // Act
            Action action = () => actual = sut.Deserialize(json);

            // Assert
            action.ShouldNotThrow();
            actual.Should().NotBeNull();
            ((string)((dynamic)actual).Prop).Should().Be(prop);
        }

        [TestMethod]
        public void sut_serializes_DateTimeOffset_property_correctly()
        {
            // Arrange
            var message = new MessageWithDateTimeOffsetProperty
            {
                DateTimeOffsetProp = fixture.Create<DateTimeOffset>()
            };
            var sut = new JsonMessageSerializer();

            // Act
            string value = sut.Serialize(message);
            TestContext.WriteLine(value);
            object actual = sut.Deserialize(value);

            // Assert
            actual.Should().BeOfType<MessageWithDateTimeOffsetProperty>();
            actual.ShouldBeEquivalentTo(message);
        }
    }
}
