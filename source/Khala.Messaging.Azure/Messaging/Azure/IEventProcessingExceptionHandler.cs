namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;

    public interface IEventProcessingExceptionHandler
    {
        Task Handle(EventProcessingExceptionContext context);
    }
}
