﻿using System;
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
            Console.WriteLine($"{nameof(Subscriber2)} - {nameof(IApplicationStartedV3)} {message.OriginalEventNumber}");
            return Task.CompletedTask;
        }

        public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
        {
            Console.WriteLine($"{nameof(Subscriber2)} - {nameof(IApplicationStartedV2)} {message.OriginalEventNumber}");
            return Task.CompletedTask;
        }

        public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
            Console.WriteLine($"{nameof(Subscriber2)} - {nameof(IApplicationStartedV1)} {message.OriginalEventNumber}");
            return Task.CompletedTask;
		}

    }
}
