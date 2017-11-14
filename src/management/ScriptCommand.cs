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
	    public static Task StartScript<TScriptData>(IEventPublisher eventPublisher, IScriptDefinition<TScriptData> scriptDefinition, TScriptData scriptData)
	    {
		    const int nextActivityIndex = 0;
		    var nextActivity = scriptDefinition.Activities[nextActivityIndex](scriptData);
		    return eventPublisher.PublishEvent
		    (
			    nextActivity,
			    x => x
				    .SetMetadataEntry(EventMetadataKey.ScriptId, Guid.NewGuid())
				    .SetMetadataEntry(EventMetadataKey.ScriptType, scriptDefinition.Type)
				    .SetMetadataEntry(EventMetadataKey.ScriptCurrentActivityIndex, nextActivityIndex)
				    .SetMetadataEntry(EventMetadataKey.ScriptData, JsonConvert.SerializeObject(scriptData))
		    );
	    }

	    public static Task ProcessNextScriptActivity<TScriptData>(IEventPublisher eventPublisher, IScriptDefinition<TScriptData> scriptDefinition, IRecordedEvent message)
	    {
		    if (!message.Metadata.TryGetValue(EventMetadataKey.ScriptType, out object candidateScriptType) || !Equals(candidateScriptType, scriptDefinition.Type))
		    {
			    return Task.CompletedTask;
		    }
		    var nextActivityIndex = Convert.ToInt32(message.Metadata[EventMetadataKey.ScriptCurrentActivityIndex]) + 1;
		    if (nextActivityIndex >= scriptDefinition.Activities.Count)
		    {
			    return Task.CompletedTask;
		    }
		    var scriptData = JsonConvert.DeserializeObject<TScriptData>(Convert.ToString(message.Metadata[EventMetadataKey.ScriptData]));
		    var nextActivity = scriptDefinition.Activities[nextActivityIndex](scriptData);
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
