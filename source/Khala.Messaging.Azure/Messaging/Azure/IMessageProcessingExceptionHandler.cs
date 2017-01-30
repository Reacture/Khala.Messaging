namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;

    public interface IMessageProcessingExceptionHandler<TSource>
        where TSource : class
    {
        Task Handle(MessageProcessingExceptionContext<TSource> context);
    }
}
