using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;
using System.Collections.ObjectModel;

namespace eventstore
{
	//public delegate Task<SubscriberConnection> ConnectSubscriber(Func<IEventStoreConnection> createConnection, Action<SubscriptionDropReason, Exception> subscriptionDropped = null);
	public class CatchUpSubscriberRegistration : ISubscriberRegistration
	{
		public EventStoreObjectName SubscriptionStreamName { get; }
		public Func<ResolvedEvent, Task> HandleEvent { get; }
		public Func<Task<long?>> GetCheckpoint { get; }
		public Func<ResolvedEvent, string> GetEventHandlingQueueKey { get; }

		public CatchUpSubscriberRegistration(EventStoreObjectName subscriptionStreamName, Func<ResolvedEvent, Task> handleEvent, Func<Task<long?>> getCheckpoint, Func<ResolvedEvent, string> getEventHandlingQueueKey)
		{
			SubscriptionStreamName = subscriptionStreamName;
			HandleEvent = handleEvent;
			GetCheckpoint = getCheckpoint;
			GetEventHandlingQueueKey = getEventHandlingQueueKey;
		}

		Task IMessageHandler<ISubscribeRegistrationHandler, Task>.Handle(ISubscribeRegistrationHandler message)
		{
			return message.Handle(this);
		}
	}

	public class VolatileSubscriberRegistration : ISubscriberRegistration
	{
		public EventStoreObjectName SubscriptionStreamName { get; }
		public Func<ResolvedEvent, Task> HandleEvent { get; }

		public VolatileSubscriberRegistration(EventStoreObjectName subscriptionStreamName, Func<ResolvedEvent, Task> handleEvent)
		{
			SubscriptionStreamName = subscriptionStreamName;
			HandleEvent = handleEvent;
		}

		Task IMessageHandler<ISubscribeRegistrationHandler, Task>.Handle(ISubscribeRegistrationHandler message)
		{
			return message.Handle(this);
		}
	}

	public class PersistentSubscriberRegistration : ISubscriberRegistration
	{
		public EventStoreObjectName SubscriptionStreamName { get; }
		public EventStoreObjectName SubscriptionGroupName { get; }
		public Func<ResolvedEvent, Task> HandleEvent { get; }

		public PersistentSubscriberRegistration(EventStoreObjectName subscriptionStreamName, EventStoreObjectName subscriptionGroupName, Func<ResolvedEvent, Task> handleEvent)
		{
			SubscriptionStreamName = subscriptionStreamName;
			SubscriptionGroupName = subscriptionGroupName;
			HandleEvent = handleEvent;
		}

		Task IMessageHandler<ISubscribeRegistrationHandler, Task>.Handle(ISubscribeRegistrationHandler message)
		{
			return message.Handle(this);
		}
	}

	public interface ISubscribeRegistrationHandler : 
		IMessageHandler<CatchUpSubscriberRegistration, Task>,
		IMessageHandler<VolatileSubscriberRegistration, Task>,
		IMessageHandler<PersistentSubscriberRegistration, Task>
	{
		
	}

	public interface ISubscriberRegistration : IMessageHandler<ISubscribeRegistrationHandler, Task>
	{
		
	}

	public interface ISubscriberRegistry : IReadOnlyDictionary<string, ISubscriberRegistration>
    {
        
    }
}
