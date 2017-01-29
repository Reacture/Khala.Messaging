namespace Arcane.Messaging
{
    public interface IMessageSerializer
    {
        string Serialize(object message);

        object Deserialize(string value);
    }
}
