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
	//todo
	public class ProvisionSubscriptionStreamScriptController :
	    IMessageHandler<IRecordedEvent<IStartProvisionSubscriptionStreamScript>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStopped>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriberStarted>, Task>,
		IMessageHandler<IRecordedEvent<ISubscriptionStreamProvisioned>, Task>
	{
		private static readonly string WorkflowType = typeof(ProvisionSubscriptionStreamScript).FullName;
		private readonly Func<ProvisionSubscriptionStreamScript.Data, object>[] _activities;
		private readonly IEventPublisher _eventPublisher;

		public ProvisionSubscriptionStreamScriptController
			(
				Func<ProvisionSubscriptionStreamScript.Data, object>[] activities, 
				IEventPublisher eventPublisher
			)
		{
			_activities = activities;
			_eventPublisher = eventPublisher;
		}

		public Task Handle(IRecordedEvent<ISubscriberStopped> message)
		{
			return ProcessNextActivity(message);
		}

		public Task Handle(IRecordedEvent<ISubscriberStarted> message)
		{
			return ProcessNextActivity(message);
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamProvisioned> message)
		{
			return ProcessNextActivity(message);
		}

		public Task Handle(IRecordedEvent<IStartProvisionSubscriptionStreamScript> message)
		{
			var scriptData = new ProvisionSubscriptionStreamScript.Data
				{
					SubscriberName = message.Data.SubscriberName,
					SubscriptionStreamName = message.Data.SubscriptionStreamName
				};
			const int nextActivityIndex = 0;
			var nextActivity = _activities[nextActivityIndex](scriptData);
			return _eventPublisher.PublishEvent
			(
				nextActivity,
				x => x
					.SetMetadataEntry(EventHeaderKey.ScriptId, message.Data.WorkflowId)
					.SetMetadataEntry(EventHeaderKey.ScriptType, WorkflowType)
					.SetMetadataEntry(EventHeaderKey.ScriptCurrentActivityIndex, nextActivityIndex)
					.SetMetadataEntry(EventHeaderKey.ScriptData, JsonConvert.SerializeObject(scriptData))
			);
		}


		private Task ProcessNextActivity<TData>(IRecordedEvent<TData> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.ScriptType, out object workflowType) || !Equals(workflowType, WorkflowType))
			{
				return Task.CompletedTask;
			}
			var nextActivityIndex = Convert.ToInt32(message.Metadata[EventHeaderKey.ScriptCurrentActivityIndex]) + 1;
			if (nextActivityIndex >= _activities.Length)
			{
				return Task.CompletedTask;
			}
			var scriptData = JsonConvert.DeserializeObject<ProvisionSubscriptionStreamScript.Data>(Convert.ToString(message.Metadata[EventHeaderKey.ScriptData]));
			var nextActivity = _activities[nextActivityIndex](scriptData);
			return _eventPublisher.PublishEvent
			(
				nextActivity,
				x => x
					.CopyMetadata(message.Metadata)
					.SetMetadataEntry(EventHeaderKey.ScriptCurrentActivityIndex, nextActivityIndex)
			);
		}
	}

	public static class ProvisionSubscriptionStreamScript
	{
		public class Data
		{
			public string SubscriptionStreamName { get; set; }
			public string SubscriberName { get; set; }
		}

		public static readonly Func<Data, object>[] Activities = 
			{
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName), 
				x => new StopSubscriber(x.SubscriberName),
				x => new ProvisionSubscriptionStream(x.SubscriptionStreamName),
				x => new StartSubscriber(x.SubscriberName),
				x => new StopSubscriber(x.SubscriberName),
				x => new StartSubscriber(x.SubscriberName)
			};
	}
}
