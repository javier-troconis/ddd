using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
	public class Subscriber2 :
        IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>,
        IMessageHandler<IRecordedEvent<IApplicationStartedV3>, Task>,
        IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>
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

	public class Subscriber2Continuation :
        IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>
	{
		public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
		{
			Console.WriteLine($"{nameof(Subscriber2Continuation)} - {message.EventStreamId} - {nameof(IApplicationStartedV2)} {message.EventId}");
			return Task.CompletedTask;
		}
	}
}
