namespace Khala.Messaging.DataAnnotations
{
    using System;
    using System.Collections;
    using System.ComponentModel.DataAnnotations;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class MessageValidator_specs
    {
        public class RootObject
        {
            [Range(1, 10)]
            public int Int32Property { get; set; } = 1;

            public StemObject StemObjectProperty { get; set; } = new StemObject();

            public IEnumerable CollectionProperty { get; set; } = new object[] { };
        }

        public class BaseObject
        {
            [Range(1, 10)]
            public int BaseInt32Property { get; set; } = 1;
        }

        public class StemObject : BaseObject
        {
            [StringLength(10)]
            public string StringProperty { get; set; } = "foo";
        }

        public class ElementObject
        {
            [Required]
            public object ObjectProperty { get; set; } = new object();
        }

        [TestMethod]
        public void given_null_argument_then_Validate_succeeds()
        {
            Action action = () => MessageValidator.Validate(instance: null);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_valid_object_then_Validate_succeeds()
        {
            var instance = new RootObject();
            Action action = () => MessageValidator.Validate(instance);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_root_has_invalid_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                Int32Property = -1,
            };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RangeAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("Int32Property");
        }

        [TestMethod]
        public void given_stem_has_invalid_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                StemObjectProperty =
                {
                    StringProperty = "f to the o to the o",
                },
            };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is StringLengthAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("StemObjectProperty.StringProperty");
        }

        [TestMethod]
        public void given_null_stem_then_Validate_succeeds()
        {
            var instance = new RootObject { StemObjectProperty = null };
            Action action = () => MessageValidator.Validate(instance);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_invalid_collection_element_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new ElementObject(),
                    new ElementObject { ObjectProperty = default },
                },
            };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RequiredAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("CollectionProperty[1].ObjectProperty");
        }

        [TestMethod]
        public void given_null_element_then_Validate_succeeds()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new ElementObject(),
                    null,
                },
            };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        [Timeout(100)]
        public void given_circular_reference_then_Validate_succeeds()
        {
            var instance = new RootObject();
            instance.CollectionProperty = new[] { instance };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_stem_has_invalid_inherited_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                StemObjectProperty =
                {
                    BaseInt32Property = -1,
                },
            };

            Action action = () => MessageValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RangeAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("StemObjectProperty.BaseInt32Property");
        }
    }
}
