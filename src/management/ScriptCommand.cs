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
    public static class ScriptCommand
    {
	    public static Task StartScript<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, TScriptData scriptData)
	    {
		    const int nextActivityIndex = 0;
		    var nextActivity = activities[nextActivityIndex](scriptData);
		    return eventPublisher.PublishEvent
		    (
			    nextActivity,
			    x => x
				    .SetMetadataEntry(EventMetadataKey.ScriptId, Guid.NewGuid())
				    .SetMetadataEntry(EventMetadataKey.ScriptType, scriptType)
				    .SetMetadataEntry(EventMetadataKey.ScriptCurrentActivityIndex, nextActivityIndex)
				    .SetMetadataEntry(EventMetadataKey.ScriptData, JsonConvert.SerializeObject(scriptData))
		    );
	    }

	    public static Task ProcessNextScriptActivity<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, IRecordedEvent message)
	    {
		    if (!message.Metadata.TryGetValue(EventMetadataKey.ScriptType, out object candidateScriptType) || !Equals(candidateScriptType, scriptType))
		    {
			    return Task.CompletedTask;
		    }
		    var nextActivityIndex = Convert.ToInt32(message.Metadata[EventMetadataKey.ScriptCurrentActivityIndex]) + 1;
		    if (nextActivityIndex >= activities.Count)
		    {
			    return Task.CompletedTask;
		    }
		    var scriptData = JsonConvert.DeserializeObject<TScriptData>(Convert.ToString(message.Metadata[EventMetadataKey.ScriptData]));
		    var nextActivity = activities[nextActivityIndex](scriptData);
		    return eventPublisher.PublishEvent
		    (
			    nextActivity,
			    x => x
				    .CopyMetadata(message.Metadata)
				    .SetMetadataEntry(EventMetadataKey.ScriptCurrentActivityIndex, nextActivityIndex)
		    );
	    }
	}
}
