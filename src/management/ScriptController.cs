using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using Newtonsoft.Json;
using shared;

namespace management
{
    public static class ScriptController
    {
	    public static Task StartScript<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, TScriptData scriptData)
	    {
		    const int nextActivityIndex = 0;
		    var nextActivity = activities[nextActivityIndex](scriptData);
		    return eventPublisher.PublishEvent
		    (
			    nextActivity,
			    x => x
				    .SetMetadataEntry(EventHeaderKey.ScriptId, Guid.NewGuid())
				    .SetMetadataEntry(EventHeaderKey.ScriptType, scriptType)
				    .SetMetadataEntry(EventHeaderKey.ScriptCurrentActivityIndex, nextActivityIndex)
				    .SetMetadataEntry(EventHeaderKey.ScriptData, JsonConvert.SerializeObject(scriptData))
		    );
	    }

	    public static Task ProcessNextScriptActivity<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, IRecordedEvent message)
	    {
		    if (!message.Metadata.TryGetValue(EventHeaderKey.ScriptType, out object candidateScriptType) || !Equals(candidateScriptType, scriptType))
		    {
			    return Task.CompletedTask;
		    }
		    var nextActivityIndex = Convert.ToInt32(message.Metadata[EventHeaderKey.ScriptCurrentActivityIndex]) + 1;
		    if (nextActivityIndex >= activities.Count)
		    {
			    return Task.CompletedTask;
		    }
		    var scriptData = JsonConvert.DeserializeObject<TScriptData>(Convert.ToString(message.Metadata[EventHeaderKey.ScriptData]));
		    var nextActivity = activities[nextActivityIndex](scriptData);
		    return eventPublisher.PublishEvent
		    (
			    nextActivity,
			    x => x
				    .CopyMetadata(message.Metadata)
				    .SetMetadataEntry(EventHeaderKey.ScriptCurrentActivityIndex, nextActivityIndex)
		    );
	    }
	}
}
