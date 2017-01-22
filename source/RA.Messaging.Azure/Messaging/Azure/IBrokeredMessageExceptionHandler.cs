namespace ReactiveArchitecture.Messaging.Azure
{
    public interface IBrokeredMessageExceptionHandler
    {
        void HandleBrokeredMessageException(HandleBrokeredMessageExceptionContext context);

        void HandleMessageException(HandleMessageExceptionContext context);
    }
}
