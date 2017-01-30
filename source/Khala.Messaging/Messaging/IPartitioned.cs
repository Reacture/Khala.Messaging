namespace Khala.Messaging
{
    public interface IPartitioned
    {
        string PartitionKey { get; }
    }
}
