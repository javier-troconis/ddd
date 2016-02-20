using shared;

namespace core
{
    public class ApplicationSubmittalCount : IEventConsumer<ApplicationSubmitted>
    {
		public int SubmittalCount { get; private set; }

	    public void Apply(ApplicationSubmitted @event)
	    {
			SubmittalCount++; 
	    }
    }
}
