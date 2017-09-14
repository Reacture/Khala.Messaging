namespace Khala.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageBusExceptionHandler
    {
        Task Handle(MessageBusExceptionContext context);
    }
}
