namespace ReactiveArchitecture.Messaging.Azure
{
    public interface IEventMessageExceptionHandler
    {
        void HandleEventException(HandleEventExceptionContext context);

        void HandleMessageException(HandleMessageExceptionContext context);
    }
}
