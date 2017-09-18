using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace eventstore
{
	public interface IEventPublisher
	{
		Task PublishEvent(object @event, Func<EventDataSettings, EventDataSettings> configureEventDataSettings = null);
	}

    public class EventPublisher : IEventPublisher
    {
	    private readonly IEventStore _eventStore;

	    public EventPublisher(IEventStore eventStore)
	    {
		    _eventStore = eventStore;
	    }

	    public async Task PublishEvent(object @event, Func<EventDataSettings, EventDataSettings> configureEventDataSettings)
	    {
		    var connectionFactory = new EventStoreConnectionFactory(
			    EventStoreSettings.ClusterDns,
			    EventStoreSettings.InternalHttpPort,
			    EventStoreSettings.Username,
			    EventStoreSettings.Password,
			    x => x
				    .WithConnectionTimeoutOf(TimeSpan.FromMinutes(1))
					);
		    var connection = connectionFactory.CreateConnection();
		    await connection.ConnectAsync();
		    var streamName = Guid.NewGuid().ToString("N");
		    var streamMetadata = StreamMetadata.Create(maxAge: TimeSpan.FromSeconds(30));
		    await connection.SetStreamMetadataAsync(streamName, ExpectedVersion.Any, streamMetadata);
			connection.Close();

			await _eventStore.WriteEvents(streamName, ExpectedVersion.Any, new[] { @event }, configureEventDataSettings);
	    }
    }
}
