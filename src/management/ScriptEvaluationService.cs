using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Newtonsoft.Json;

namespace management
{
	public delegate ScriptActivity<TScriptData> GetNextScriptActivity<TScriptData>(IReadOnlyList<ScriptActivity<TScriptData>> scriptActivities, TScriptData scriptData, Guid? currentActivityId = null);

	public struct ScriptExecutionContext<TScriptData>
	{
		public ScriptExecutionContext(string scriptType, Guid scriptInstanceId, long expectedVersion, TScriptData scriptData, Guid activityId, string activityName)
		{
			ScriptType = scriptType;
			ScriptInstanceId = scriptInstanceId;
			ScriptData = scriptData;
			ActivityId = activityId;
			ActivityName = activityName;
			ExpectedVersion = expectedVersion;
		}

		public string ScriptType { get; }
		public Guid ScriptInstanceId { get; }
		public long ExpectedVersion { get; }
		public TScriptData ScriptData { get; }
		public Guid ActivityId { get; }
		public string ActivityName { get; }
	}

	public interface IScriptEvaluationService
	{
		Task StartScript<TScriptData>(Guid scriptInstanceId, IScriptDefinition<TScriptData> scriptDefinition, GetNextScriptActivity<TScriptData> getNextScriptActivity, TScriptData scriptData);

		Task StartNextScriptActivity<TScriptData>(IScriptDefinition<TScriptData> scriptDefinition, GetNextScriptActivity<TScriptData> getNextScriptActivity, ScriptExecutionContext<TScriptData> scriptExecutionContext);
	}

	public class ScriptEvaluationService : IScriptEvaluationService
	{
		private readonly IEventPublisher _eventPublisher;

		public ScriptEvaluationService(IEventPublisher eventPublisher)
		{
			_eventPublisher = eventPublisher;
		}

		public async Task StartScript<TScriptData>(Guid scriptInstanceId, IScriptDefinition<TScriptData> scriptDefinition, GetNextScriptActivity<TScriptData> getNextScriptActivity, TScriptData scriptData)
		{
			var nextActivity = getNextScriptActivity(scriptDefinition.Activities, scriptData);
			if (nextActivity.Equals(default(ScriptActivity<TScriptData>)))
			{
				return;
			}
			Console.WriteLine("Started script: " + scriptInstanceId);
			Console.WriteLine("Running script activity: " + nextActivity.ActivityName);
			var nextActivityRequest = nextActivity.GetActivityRequest(scriptData);
			await _eventPublisher.PublishEvent
			(
				nextActivityRequest,
				x => x
					.SetMetadataEntry
					(
						EventMetadataKey.ScriptExecutionContext,
						JsonConvert.SerializeObject
						(
							new ScriptExecutionContext<TScriptData>
							(
								scriptDefinition.ScriptType,
								scriptInstanceId,
								0,
								scriptData,
								nextActivity.ActivityId,
								nextActivity.ActivityName
							)
						)
					),
				streamName: $"{scriptDefinition.ScriptType}_{scriptInstanceId:N}",
				expectedVersion: -1
			);
		}

		public async Task StartNextScriptActivity<TScriptData>(IScriptDefinition<TScriptData> scriptDefinition, GetNextScriptActivity<TScriptData> getNextScriptActivity, ScriptExecutionContext<TScriptData> scriptExecutionContext)
		{
			if (!Equals(scriptExecutionContext.ScriptType, scriptDefinition.ScriptType))
			{
				return;
			}
			var nextActivity = getNextScriptActivity(scriptDefinition.Activities, scriptExecutionContext.ScriptData, scriptExecutionContext.ActivityId);
			if (nextActivity.Equals(default(ScriptActivity<TScriptData>)))
			{
				Console.WriteLine("Finished script: " + scriptExecutionContext.ScriptInstanceId);
				return;
			}
			Console.WriteLine("Running script activity: " + nextActivity.ActivityName);
			var nextActivityRequest = nextActivity.GetActivityRequest(scriptExecutionContext.ScriptData);
			try
			{
				await _eventPublisher.PublishEvent
				(
					nextActivityRequest,
					x => x
						.SetMetadataEntry
						(
							EventMetadataKey.ScriptExecutionContext,
							JsonConvert.SerializeObject
							(
								new ScriptExecutionContext<TScriptData>
								(
									scriptExecutionContext.ScriptType,
									scriptExecutionContext.ScriptInstanceId,
									scriptExecutionContext.ExpectedVersion + 1,
									scriptExecutionContext.ScriptData,
									nextActivity.ActivityId,
									nextActivity.ActivityName
								)
							)
						),
					streamName: $"{scriptExecutionContext.ScriptType}_{scriptExecutionContext.ScriptInstanceId:N}",
					expectedVersion: scriptExecutionContext.ExpectedVersion
				);
			}
			catch (WrongExpectedVersionException)
			{
				
			}
		}
	}
}
