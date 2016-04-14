using shared;

namespace core
{
    public class ApplicationSubmitState : IEventConsumer<ApplicationSubmitted>
    {
		public bool HasBeenSubmitted { get; private set; }

	    public void Apply(ApplicationSubmitted @event)
	    {
			HasBeenSubmitted = true; 
	    }
    }
}
