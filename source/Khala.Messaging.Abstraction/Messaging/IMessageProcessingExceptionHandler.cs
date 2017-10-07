namespace Khala.Messaging
{
    using System.Threading.Tasks;

    public interface IMessageProcessingExceptionHandler<TData>
        where TData : class
    {
        Task Handle(MessageProcessingExceptionContext<TData> exceptionContext);
    }
}
