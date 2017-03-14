using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber1 : 
	
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
    {
	    private readonly SendEmail _sendEmail;

	    public Subscriber1(SendEmail sendEmail)
	    {
		    _sendEmail = sendEmail;
	    }

	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
		    return _sendEmail("sysadmin", message.Event.SubmittedBy);
		}
    }
}
