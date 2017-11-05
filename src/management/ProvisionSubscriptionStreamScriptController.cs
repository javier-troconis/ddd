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
			var data = new ProvisionSubscriptionStreamScript.Data
				{
					SubscriberName = message.Data.SubscriberName,
					SubscriptionStreamName = message.Data.SubscriptionStreamName
				};
			const int currentActivityIndex = 0;
			return _eventPublisher.PublishEvent
			(
				_activities[currentActivityIndex](data),
				x => x
					.SetMetadataEntry(EventHeaderKey.WorkflowId, message.Data.WorkflowId)
					.SetMetadataEntry(EventHeaderKey.WorkflowType, WorkflowType)
					.SetMetadataEntry(EventHeaderKey.WorkflowCurrentActivityIndex, currentActivityIndex)
					.SetMetadataEntry(EventHeaderKey.WorkflowData, JsonConvert.SerializeObject(data))
			);
		}


		private Task ProcessNextActivity<TData>(IRecordedEvent<TData> message)
		{
			if (!message.Metadata.TryGetValue(EventHeaderKey.WorkflowType, out object workflowType) || !Equals(workflowType, WorkflowType))
			{
				return Task.CompletedTask;
			}
			var nextActivityIndex = (int) message.Metadata[EventHeaderKey.WorkflowCurrentActivityIndex] + 1;
			if (nextActivityIndex >= _activities.Length)
			{
				return Task.CompletedTask;
			}
			var data = JsonConvert.DeserializeObject<ProvisionSubscriptionStreamScript.Data>((string)message.Metadata[EventHeaderKey.WorkflowData]);
			return _eventPublisher.PublishEvent
			(
				_activities[nextActivityIndex](data),
				x => x
					.CopyMetadata(message.Metadata)
					.SetMetadataEntry(EventHeaderKey.WorkflowCurrentActivityIndex, nextActivityIndex)
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
