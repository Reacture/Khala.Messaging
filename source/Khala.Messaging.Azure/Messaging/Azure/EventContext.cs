namespace Khala.Messaging.Azure
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Azure.EventHubs;

    /// <summary>
    /// Encapsulates data for event message processing.
    /// </summary>
    public sealed class EventContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventContext"/> class.
        /// </summary>
        /// <param name="envelope">An <see cref="Envelope"/> that contains the message object and related properties.</param>
        /// <param name="properties">A property bag from <see cref="EventData"/>.</param>
        public EventContext(
            Envelope envelope, IDictionary<string, object> properties)
        {
            Envelope = envelope ?? throw new ArgumentNullException(nameof(envelope));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        /// <summary>
        /// Gets the <see cref="Envelope"/> that contains a message.
        /// </summary>
        /// <value>
        /// The <see cref="Envelope"/> that contains a message.
        /// </value>
        public Envelope Envelope { get; }

        /// <summary>
        /// Gets the property bag.
        /// </summary>
        /// <value>
        /// The property bag.
        /// </value>
        /// <remarks>Generally initialized with properties from <see cref="EventData.Properties"/>.</remarks>
        public IDictionary<string, object> Properties { get; }
    }
}
