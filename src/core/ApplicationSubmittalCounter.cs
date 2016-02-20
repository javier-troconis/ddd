using shared;

namespace core
{
    public class ApplicationSubmittalCounter : IEventConsumer<ApplicationSubmitted>
    {
		public int SubmittalCount { get; private set; }

	    public void Apply(ApplicationSubmitted @event)
	    {
			SubmittalCount++; 
	    }
    }
}
