namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IMessageSender
    {
        Task Send(IReadOnlyCollection<MessageContent> messages);
    }
}
