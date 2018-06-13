namespace Khala.Messaging.Azure
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.EventHubs;

    public class EventHubMessageSender : IMessageSender
    {
        private readonly IMessageSerializer _serializer;
        private readonly EventHubClient _eventHubClient;

        public EventHubMessageSender(
            IMessageSerializer serializer, EventHubClient eventHubClient)
        {
            _serializer = serializer;
            _eventHubClient = eventHubClient;
        }

        public Task Send(IReadOnlyCollection<MessageContent> messages)
        {
            return Task.CompletedTask;
        }
    }
}
