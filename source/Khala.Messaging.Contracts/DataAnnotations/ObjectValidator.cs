namespace Khala.DataAnnotations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides a helper class that can be used to validate objects. Unlike <see cref="Validator"/>, <see cref="ObjectValidator"/> traverses an object graph and uses full paths of properties as member names for <see cref="ValidationResult"/>.
    /// </summary>
    public static class ObjectValidator
    {
        /// <summary>
        /// Determines whether the specified object is valid. If not valid a <see cref="ValidationException"/> is thrown.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <exception cref="ValidationException">
        /// The object is not valid.
        /// </exception>
        public static void Validate(object instance)
        {
            Visitor.Visit(
                instance,
                breakOnFirstError: true,
                onError: error => throw error.ToException());
        }

        /// <summary>
        /// Determine whether the specified object is valid. If the object is valid <paramref name="validationResult"/> is set to <see cref="ValidationResult.Success"/>; otherwise, a <see cref="ValidationResult"/> that contains error data.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="validationResult">A <see cref="ValidationResult"/> object.</param>
        /// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
        public static bool TryValidate(
            object instance,
            out ValidationResult validationResult)
        {
            ValidationResult captured = ValidationResult.Success;

            Visitor.Visit(
                instance,
                breakOnFirstError: true,
                onError: error => captured = error.ValidationResult);

            validationResult = captured;
            return validationResult == ValidationResult.Success;
        }

        /// <summary>
        /// Determine whether the specified object is valid using a validation results collector.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="validationResultCollector">A callback function called for each <see cref="ValidationResult"/> object indicating validation error.</param>
        /// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
        public static bool TryValidate(
            object instance,
            Action<ValidationResult> validationResultCollector)
        {
            if (validationResultCollector == null)
            {
                throw new ArgumentNullException(nameof(validationResultCollector));
            }

            bool hasError = false;

            Visitor.Visit(instance, breakOnFirstError: false, onError: error =>
            {
                hasError = true;
                validationResultCollector.Invoke(error.ValidationResult);
            });

            return hasError == false;
        }

        private static IEnumerable<PropertyInfo> GetProperties(object instance)
        {
            return from type in AscendTypeHierarchy(instance)
                   from property in GetDeclaredProperties(type)
                   select property;
        }

        private static IEnumerable<Type> AscendTypeHierarchy(object instance)
        {
            Type type = instance.GetType();
            while (type != null)
            {
                yield return type;
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

        private struct ValidationError
        {
            public ValidationError(
                ValidationAttribute validationAttribute,
                ValidationResult validationResult,
                object value)
            {
                ValidationAttribute = validationAttribute;
                ValidationResult = validationResult;
                Value = value;
            }

            public ValidationAttribute ValidationAttribute { get; }

            public ValidationResult ValidationResult { get; }

            public object Value { get; }

            public ValidationException ToException()
                => new ValidationException(ValidationResult, ValidationAttribute, Value);
        }

        private class Visitor
        {
            private readonly Stack<object> _history;
            private readonly bool _breakOnFirstError;
            private readonly Action<ValidationError> _onError;
            private bool _hasError;

            private Visitor(bool breakOnFirstError, Action<ValidationError> onError)
            {
                _history = new Stack<object>();
                _breakOnFirstError = breakOnFirstError;
                _onError = onError;
                _hasError = false;
            }

            private bool ShouldBreak => _hasError && _breakOnFirstError;

            public static void Visit(
                object instance,
                bool breakOnFirstError,
                Action<ValidationError> onError)
            {
                new Visitor(breakOnFirstError, onError).Visit(instance, prefix: string.Empty);
            }

            private void Visit(object instance, string prefix)
            {
                if (instance == null)
                {
                    return;
                }

                if (Visited(instance) == false)
                {
                    LeaveVisitRecord(instance);
                    VisitProperties(instance, prefix);
                    if (HasElements(instance, out IEnumerable enumerable))
                    {
                        VisitElements(enumerable, prefix);
                    }
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
                    if (ShouldBreak)
                    {
                        return;
                    }

                    ValidateProperty(instance, value, memberName, validator);
                }

                Visit(value, prefix: memberName);
            }

            private void ValidateProperty(
                object instance, object value, string memberName, ValidationAttribute validator)
            {
                var validationContext = new ValidationContext(instance) { MemberName = memberName };
                ValidationResult result = validator.GetValidationResult(value, validationContext);
                if (result != ValidationResult.Success)
                {
                    _hasError = true;
                    _onError.Invoke(new ValidationError(validator, result, value));
                }
            }

            private static bool HasElements(object instance, out IEnumerable enumerable)
            {
                enumerable = instance as IEnumerable;
                return enumerable != null && enumerable is string == false;
            }

            private void VisitElements(IEnumerable enumerable, string prefix)
            {
                int index = 0;
                foreach (object element in enumerable)
                {
                    string elementPrefix = $"{prefix}[{index}]";
                    Visit(element, elementPrefix);
                    index++;
                }
            }
        }
    }
}
