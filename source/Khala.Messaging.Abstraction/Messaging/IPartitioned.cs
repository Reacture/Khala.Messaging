namespace Khala.Messaging
{
    /// <summary>
    /// Represents a partitioned message.
    /// </summary>
    public interface IPartitioned
    {
        /// <summary>
        /// Gets the partition key of the message.
        /// </summary>
        /// <value>
        /// The partition key of the message.
        /// </value>
        string PartitionKey { get; }
    }
}
