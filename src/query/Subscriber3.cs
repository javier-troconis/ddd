using System;
using System.Threading.Tasks;

using command.contracts;

using shared;
using eventstore;

namespace query
{
    public class Subscriber3 :
        IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>, 
		IMessageHandler<IRecordedEvent<IIdentityVerificationCompletedV1>, Task>,
	    IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
	{
        public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
        {
            Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IApplicationStartedV1)}");
            return Task.CompletedTask;
        }

		public Task Handle(IRecordedEvent<IIdentityVerificationCompletedV1> message)
		{
			Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IIdentityVerificationCompletedV1)}");
			return Task.CompletedTask;
		}

		public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
		{
			Console.WriteLine($"{nameof(Subscriber3)} - {message.EventStreamId} - {nameof(IApplicationSubmittedV1)}");
			return Task.CompletedTask;
		}
	}
}
