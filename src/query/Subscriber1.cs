using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
	public class Subscriber1 :
		IMessageHandler<IRecordedEvent<IApplicationStarted_V1>, Task>
	{
		public Task Handle(IRecordedEvent<IApplicationStarted_V1> message)
		{
			Console.WriteLine($"{nameof(Subscriber1)} - {message.EventStreamId} - {nameof(IApplicationStarted_V1)}");
			return Task.CompletedTask;
		}
	}
}
