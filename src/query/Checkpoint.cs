using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;

namespace query
{
    public static class Checkpoint<TSubscriber> where TSubscriber : IMessageHandler
    {
	    public static readonly Func<ResolvedEvent, Task<ResolvedEvent>> WriteCheckpoint =
		    resolvedEvent =>
		    {
			    var filePath = typeof(TSubscriber).Name + ".checkpoint.tmp";
				File.WriteAllText(filePath, resolvedEvent.OriginalEventNumber.ToString());
			    return Task.FromResult(resolvedEvent);
		    };

	    public static readonly Func<Task<long?>> ReadCheckpoint =
		    () =>
		    {
			    var filePath = typeof(TSubscriber).Name + ".checkpoint.tmp";
				var checkpoint = File.Exists(filePath) ?
					long.Parse(File.ReadAllText(filePath)) : default(long?);
			    return Task.FromResult(checkpoint);
		    };
	}
}
