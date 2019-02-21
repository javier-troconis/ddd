using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace management
{
	public static class ScriptFlowType
	{
		public static ScriptActivity<TScriptData> AscendingSequence<TScriptData>(IReadOnlyList<ScriptActivity<TScriptData>> scriptActivities, TScriptData scriptData, Guid? currentActivityId = null)
		{
			if (currentActivityId == null)
			{
				return scriptActivities[0];
			}
			var nextActivityIndex = Array.IndexOf(scriptActivities.Select(x => x.ActivityId).ToArray(), currentActivityId) + 1;
			return nextActivityIndex >= scriptActivities.Count ? default(ScriptActivity<TScriptData>) : scriptActivities[nextActivityIndex];
		}
	}
}
