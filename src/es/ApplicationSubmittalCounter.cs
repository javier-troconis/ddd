using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
    public class ApplicationSubmittalCounter : IEventConsumer<ApplicationSubmitted>
    {
		public int Submittals { get; private set; }

	    public void Apply(ApplicationSubmitted @event)
	    {
			Submittals++; 
	    }
    }
}
