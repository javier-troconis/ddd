using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
    public class Subscriber3 :
        IMessageHandler<IRecordedEvent<IApplicationStarted_V1>, Task>, 
		IMessageHandler<IRecordedEvent<IIdentityVerificationCompleted_V1>, Task>,
	    IMessageHandler<IRecordedEvent<IApplicationSubmitted_V1>, Task>
	{
        public Task Handle(IRecordedEvent<IApplicationStarted_V1> message)
        {
            Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IApplicationStarted_V1)}");
            return Task.CompletedTask;
        }

		public Task Handle(IRecordedEvent<IIdentityVerificationCompleted_V1> message)
		{
			Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IIdentityVerificationCompleted_V1)}");
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IApplicationSubmitted_V1> message)
		{
			Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IApplicationSubmitted_V1)}");
			return Task.CompletedTask;
		}
	}
}
