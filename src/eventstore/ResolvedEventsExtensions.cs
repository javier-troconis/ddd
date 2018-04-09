using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace eventstore
{
    public static class ResolvedEventsExtensions
    {
	    public static T Aggregate<T>(this IEnumerable<ResolvedEvent> events, T seed = default(T)) where T : IMessageHandler, new()
	    {
		    return events.Aggregate
				(Equals(seed, default(T)) ? new T() : seed,
					ResolvedEventHandleFactory.CreateResolvedEventHandle<T>()
		    );
		}
	}
}
