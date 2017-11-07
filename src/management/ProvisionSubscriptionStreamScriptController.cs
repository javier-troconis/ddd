using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using management.contracts;
using Newtonsoft.Json;
using shared;

namespace management
{
	public class ProvisionSubscriptionStreamScriptController :
	    IMessageHandler<IRecordedEvent<IStartProvisionSubscriptionStreamScript>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStreamProvisioned>, Task>
	{
		private static readonly string ScriptType = typeof(ProvisionSubscriptionStreamScript).FullName;
		private readonly IReadOnlyList<Func<ProvisionSubscriptionStreamScript.Data, object>> _activities;
		private readonly IEventPublisher _eventPublisher;

		public ProvisionSubscriptionStreamScriptController
			(
				IReadOnlyList<Func<ProvisionSubscriptionStreamScript.Data, object>> activities, 
				IEventPublisher eventPublisher
			)
		{
			_activities = activities;
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			return ProcessNextScriptActivity(_eventPublisher, _activities, ScriptType, message);
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			return ProcessNextScriptActivity(_eventPublisher, _activities, ScriptType, message);
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			return ProcessNextScriptActivity(_eventPublisher, _activities, ScriptType, message);
		}

		public Task Handle(IRecordedEvent<IStartProvisionSubscriptionStreamScript> message)
		{
			return StartScript
				(
					_eventPublisher, 
					_activities,
					ScriptType,
					new ProvisionSubscriptionStreamScript.Data
					{
						SubscriberName = message.Data.SubscriberName,
						SubscriptionStreamName = message.Data.SubscriptionStreamName
					}
				);
		}

		private static Task StartScript<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, TScriptData scriptData)
		{
			const int nextActivityIndex = 0;
			var nextActivity = activities[nextActivityIndex](scriptData);
			Console.WriteLine(nextActivity.GetType());
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

		private static Task ProcessNextScriptActivity<TScriptData>(IEventPublisher eventPublisher, IReadOnlyList<Func<TScriptData, object>> activities, string scriptType, IRecordedEvent message)
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
			Console.WriteLine(nextActivity.GetType());
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
