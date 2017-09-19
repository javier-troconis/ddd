using System;
using System.Threading.Tasks;

using command.contracts;

using shared;

namespace query
{
	public class Subscriber2 : 
		IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationStartedV3>, Task>
	{
        public Task Handle(IRecordedEvent<IApplicationStartedV3> message)
        {
            Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStartedV3)} {message.EventId}");
            return Task.CompletedTask;
        }

        public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
        {
            Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStartedV2)} {message.EventId}");
            return Task.CompletedTask;
        }

        public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
            Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStartedV1)} {message.EventId}");
            return Task.CompletedTask;
		}

    }
}
