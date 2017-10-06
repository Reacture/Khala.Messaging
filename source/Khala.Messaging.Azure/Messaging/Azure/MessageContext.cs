namespace Khala.Messaging.Azure
{
    using System;
    using System.Threading.Tasks;

    public class MessageContext<TData>
        where TData : class
    {
        private readonly TData _data;
        private readonly Func<Task> _acknowledge;

        public MessageContext(TData data, Func<Task> acknowledge)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _acknowledge = acknowledge ?? throw new ArgumentNullException(nameof(acknowledge));
        }

        public TData Data => _data;

        public Task Acknowledge() => _acknowledge.Invoke();
    }
}
