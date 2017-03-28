using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IEventPublisher
	{
		Task PublishEvent(object @event, Action<object, IDictionary<string, object>> configureEventHeader = null);
	}

    public class EventPublisher : IEventPublisher
    {
	    private readonly IEventStore _eventStore;

	    public EventPublisher(IEventStore eventStore)
	    {
		    _eventStore = eventStore;
	    }

	    public Task PublishEvent(object @event, Action<object, IDictionary<string, object>> configureEventHeader)
	    {
		    return _eventStore.WriteEvents(@event.GetType().GetEventStoreName(), ExpectedVersion.Any, new[] { @event }, configureEventHeader);
	    }
    }
}
