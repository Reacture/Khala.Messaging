namespace Khala.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MessageBusExceptionContext
    {
        public MessageBusExceptionContext(IEnumerable<Envelope> envelopes, Exception exception)
        {
            if (envelopes == null)
            {
                throw new ArgumentNullException(nameof(envelopes));
            }

            List<Envelope> envelopeList = envelopes.ToList();
            if (envelopeList.Count == 0)
            {
                throw new ArgumentException(
                    $"The argument '{envelopes}' cannot be empty.",
                    nameof(envelopes));
            }

            for (int i = 0; i < envelopeList.Count; i++)
            {
                Envelope envelope = envelopeList[i];
                if (envelope == null)
                {
                    throw new ArgumentException(
                        $"The argument '{envelopes}' cannot contain null.",
                        nameof(envelopes));
                }
            }

            Envelopes = envelopeList;
            Exception = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        public IReadOnlyList<Envelope> Envelopes { get; }

        public Exception Exception { get; }

        public bool Handled { get; set; } = false;
    }
}
