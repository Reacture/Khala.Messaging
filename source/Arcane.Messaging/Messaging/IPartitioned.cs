namespace Arcane.Messaging
{
    public interface IPartitioned
    {
        string PartitionKey { get; }
    }
}
