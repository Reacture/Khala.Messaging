namespace Khala.Messaging
{
    /// <summary>
    /// Represents a message serializer that serialize and deserialize message objects into and from <see cref="string"/>.
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// Serializes a message object into <see cref="string"/>.
        /// </summary>
        /// <param name="message">A message object to serialize.</param>
        /// <returns>A <see cref="string"/> that contains serialized data.</returns>
        string Serialize(object message);

        /// <summary>
        /// Deserializes a message object from <see cref="string"/>
        /// </summary>
        /// <param name="value">A <see cref="string"/> that contains serialized data.</param>
        /// <returns>A message object deserialized.</returns>
        object Deserialize(string value);
    }
}
