namespace Khala.Messaging
{
    using System;

    /// <summary>
    /// Represents an envelope that contains properties for messaging.
    /// </summary>
    public interface IEnvelope
    {
        /// <summary>
        /// Gets the identifier of the message.
        /// </summary>
        /// <value>
        /// The identifier of the message.
        /// </value>
        Guid MessageId { get; }

        /// <summary>
        /// Gets the identifier of the operation.
        /// </summary>
        /// <value>
        /// The identifier of the operation.
        /// </value>
        string OperationId { get; }

        /// <summary>
        /// Gets the identifier of the correlation.
        /// </summary>
        /// <value>
        /// The identifier of the correlation.
        /// </value>
        Guid? CorrelationId { get; }

        /// <summary>
        /// Gets information of the contributor to the message.
        /// </summary>
        /// <value>
        /// Information of the contributor to the message.
        /// </value>
        string Contributor { get; }
    }
}
