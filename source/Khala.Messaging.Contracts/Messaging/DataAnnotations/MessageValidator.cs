namespace Khala.Messaging.DataAnnotations
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using Khala.DataAnnotations;

    /// <summary>
    /// Provides a helper class that can be used to validate message objects. Unlike <see cref="Validator"/>, <see cref="MessageValidator"/> traverses an object graph and uses full paths of properties as member names for <see cref="ValidationResult"/>.
    /// </summary>
    [Obsolete("Use Khala.DataAnnotations.ObjectValidator class instead. This class will be removed in version 1.0.0.")]
    public static class MessageValidator
    {
        /// <summary>
        /// Determines whether the specified message object is valid.
        /// </summary>
        /// <param name="instance">The message object to validate.</param>
        public static void Validate(object instance) => ObjectValidator.Validate(instance);
    }
}
