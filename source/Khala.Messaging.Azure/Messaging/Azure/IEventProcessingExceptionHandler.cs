namespace Khala.Messaging.Azure
{
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an exception handler to handle exceptions thrown while processing event data.
    /// </summary>
    public interface IEventProcessingExceptionHandler
    {
        /// <summary>
        /// Handles exceptions thrown while processing event data.
        /// </summary>
        /// <param name="context">An <see cref="EventProcessingExceptionContext"/> that encapsulates information related to an error occured while processing event data.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Handle(EventProcessingExceptionContext context);
    }
}
