namespace Khala.Messaging.DataAnnotations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides a helper class that can be used to validate message objects. Unlike <see cref="Validator"/>, <see cref="MessageValidator"/> traverses an object graph and uses full paths of properties as member names for <see cref="ValidationResult"/>.
    /// </summary>
    [Obsolete("Use Khala.Messaging.DataAnnotations.ObjectValidator class instead. This class will be removed in version 1.0.0.")]
    public static class MessageValidator
    {
        /// <summary>
        /// Determines whether the specified message object is valid.
        /// </summary>
        /// <param name="instance">The message object to validate.</param>
        public static void Validate(object instance) => Visitor.Visit(instance);

        private static IEnumerable<PropertyInfo> GetProperties(object instance)
        {
            Type type = instance.GetType();
            while (type != null)
            {
                foreach (PropertyInfo property in GetDeclaredProperties(type))
                {
                    yield return property;
                }

                type = type.GetTypeInfo().BaseType;
            }
        }

        private static IEnumerable<PropertyInfo> GetDeclaredProperties(Type type)
        {
            return from p in type.GetTypeInfo().DeclaredProperties
                   where IsIndex(p) == false
                   select p;
        }

        private static bool IsIndex(PropertyInfo property) => property.GetIndexParameters().Any();

        private static IEnumerable<ValidationAttribute> GetValidators(PropertyInfo property)
            => property.GetCustomAttributes<ValidationAttribute>();

        private static void ValidateValue(
            object instance,
            object value,
            string memberName,
            ValidationAttribute validator)
        {
            var validationContext = new ValidationContext(instance) { MemberName = memberName };
            ValidationResult result = validator.GetValidationResult(value, validationContext);
            if (result != ValidationResult.Success)
            {
                throw new ValidationException(result, validator, value);
            }
        }

        private class Visitor
        {
            private readonly Stack<object> _history;

            private Visitor() => _history = new Stack<object>();

            public static void Visit(object instance)
                => new Visitor().Visit(instance, prefix: string.Empty);

            private void Visit(object instance, string prefix)
            {
                if (instance == null || Visited(instance))
                {
                    return;
                }

                LeaveVisitRecord(instance);

                VisitProperties(instance, prefix);

                if (instance is IEnumerable collection &&
                    instance is string == false)
                {
                    VisitElements(collection, prefix);
                }
            }

            private bool Visited(object instance)
                => _history.Any(x => ReferenceEquals(x, instance));

            private void LeaveVisitRecord(object instance)
                => _history.Push(instance);

            private void VisitProperties(object instance, string prefix)
            {
                foreach (PropertyInfo property in GetProperties(instance))
                {
                    string memberName = string.IsNullOrWhiteSpace(prefix)
                        ? property.Name
                        : $"{prefix}.{property.Name}";
                    VisitProperty(instance, property, memberName);
                }
            }

            private void VisitProperty(
                object instance, PropertyInfo property, string memberName)
            {
                object value = property.GetValue(instance);
                foreach (ValidationAttribute validator in GetValidators(property))
                {
                    ValidateValue(instance, value, memberName, validator);
                }

                Visit(value, prefix: memberName);
            }

            private void VisitElements(IEnumerable collection, string prefix)
            {
                int index = 0;
                foreach (object element in collection)
                {
                    string elementPrefix = $"{prefix}[{index}]";
                    Visit(element, elementPrefix);
                    index++;
                }
            }
        }
    }
}
