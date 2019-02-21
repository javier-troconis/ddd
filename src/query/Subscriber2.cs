using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
	public class Subscriber2 :
		IMessageHandler<IRecordedEvent<IApplicationStarted_V1>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationStarted_V3>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationStarted_V2>, Task>
	{
		public Task Handle(IRecordedEvent<IApplicationStarted_V3> message)
		{
			Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStarted_V3)}");
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IApplicationStarted_V2> message)
		{
			Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStarted_V2)}");
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IApplicationStarted_V1> message)
		{
			Console.WriteLine($"{nameof(Subscriber2)} - {message.EventStreamId} - {nameof(IApplicationStarted_V1)}");
			return Task.CompletedTask;
		}
	}

}
