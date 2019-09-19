using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.api.messaging.host
{
    public class MessageHandlerResultToEventService<T1> : IMessageHandler<T1, Task<IEnumerable<Event>>>
    {
        private readonly IMessageHandler<T1, Task<IEnumerable<Event>>> _service;

        public MessageHandlerResultToEventService(IMessageHandler<T1, Task<IEnumerable<Event>>> service)
        {
            _service = service;
        }

        public async Task<IEnumerable<Event>> Handle(T1 message)
        {
            try
            {
                return await _service.Handle(message);
            }
            catch (Exception ex)
            {
                var data = new
                {
                    Reason = ex.Message
                };
                return new[]
                {
                    new Event("operationfailed", data)
                };
            }
        }
    }
}
