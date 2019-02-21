using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace management
{
	public delegate object GetActivityRequest<in TScriptData>(TScriptData scriptData);

	public struct ScriptActivity<TScriptData>
	{
		public ScriptActivity(Guid activityId, string activityName, GetActivityRequest<TScriptData> getActivityRequest)
		{
			ActivityId = activityId;
			ActivityName = activityName;
			GetActivityRequest = getActivityRequest;
		}

		public Guid ActivityId { get; }
		public string ActivityName { get; }
		public GetActivityRequest<TScriptData> GetActivityRequest { get; }
	}

	public interface IScriptDefinition<TScriptData>
	{
		string ScriptType { get; }
		IReadOnlyList<ScriptActivity<TScriptData>> Activities { get; }
	}
}
