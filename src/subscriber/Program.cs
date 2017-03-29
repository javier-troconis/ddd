using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

using ImpromptuInterface;

using management.contracts;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using shared;

namespace subscriber
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var projectionManager = new ProjectionManager(
				EventStoreSettings.ClusterDns,
				EventStoreSettings.ExternalHttpPort,
				EventStoreSettings.Username,
				EventStoreSettings.Password,
				new ConsoleLogger());

			ISubscriptionProjectionRegistry subscriptionProjectionRegistry = new ProjectionRegistry(projectionManager);

			var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns, 
                EventStoreSettings.InternalHttpPort, 
                EventStoreSettings.Username, 
                EventStoreSettings.Password);

			var persistentSubscriptionManager = new PersistentSubscriptionManager(connectionFactory.CreateConnection);
			var persistentSubscriptionRegistry = new PersistentSubscriptionRegistry(persistentSubscriptionManager);


			Parallel.For(1, 2, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, x =>
				  new EventBus(connectionFactory.CreateConnection)
					.RegisterCatchupSubscriber(
						new Subscriber2(),
							() => Task.FromResult(default(long?)),
							_writeCheckpoint.ToAsyncInput().ComposeBackward)
					.RegisterCatchupSubscriber(
						new Subscriber1(),
							() => Task.FromResult(default(long?)),
							_writeCheckpoint.ToAsyncInput().ComposeBackward)
					.RegisterPersistentSubscriber(new Subscriber3())
					.RegisterPersistentSubscriber<IRegisterSubscriptionProjectionHandler>(new RegisterSubscriptionProjectionHandler("*", subscriptionProjectionRegistry))
					.RegisterVolatileSubscriber<IRegisterPersistentSubscriptionHandler>(new RegisterPersistentSubscriptionHandler("*", persistentSubscriptionRegistry))
					.Start()
			);

			while (true)
			{

			}
		}

		private static readonly Func<ResolvedEvent, Task<ResolvedEvent>> _writeCheckpoint = resolvedEvent =>
		{
			Console.WriteLine("checkpointing - " + resolvedEvent.OriginalEventNumber);
			return Task.FromResult(resolvedEvent);
		};
	}
}
