using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace management
{
	public static class Convert
	{
		public static ScriptExecutionContext<TScriptData> ToScriptExecutionContext<TScriptData>(IReadOnlyDictionary<string, object> messageMetadata)
		{
			return messageMetadata.TryGetValue(EventMetadataKey.ScriptExecutionContext, out var data) ?
				JsonConvert.DeserializeObject<ScriptExecutionContext<TScriptData>>((string)data) : 
					default(ScriptExecutionContext<TScriptData>);
		}
	}
}
