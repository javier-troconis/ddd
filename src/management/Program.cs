﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

using management.contracts;

namespace management
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

            var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password);

            var connection = connectionFactory.CreateConnection();
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));
            connection.ConnectAsync().Wait();

            while (true)
            {
                Console.WriteLine("1 - register topics projection");
                Console.WriteLine("2 - register persistent subscription handler projection");
				Console.WriteLine("3 - register subscription projection handler projection");
				Console.WriteLine("4 - register persistent subscription");
				Console.WriteLine("5 - register subscription projection");
				var key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '1':
                        ProjectionRegistry.RegisterTopicsProjection(projectionManager).Wait();
                        break;
                    case '2':
                        ProjectionRegistry.RegisterSubscriptionProjection<IRegisterPersistentSubscriptionHandler>(projectionManager).Wait();
                        break;
					case '3':
						ProjectionRegistry.RegisterSubscriptionProjection<IRegisterSubscriptionProjectionHandler>(projectionManager).Wait();
						break;
					case '4':
                        eventPublisher.PublishEvent(new RegisterPersistentSubscription("*", "*")).Wait();
                        break;
					case '5':
						eventPublisher.PublishEvent(new RegisterSubscriptionProjection("*", "*")).Wait();
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


