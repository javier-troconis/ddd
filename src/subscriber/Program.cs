using infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using contracts;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace subscriber
{
    public class Program
    {


        private const string _subscriptionGroupName = "application1";
        private const string _subscriptionStreamName = "application1";
        private static readonly string[] _eventTypes = { "applicationsubmitted", "applicationstarted" };

        public static void Main(string[] args)
        {
            var projectionManager = new ProjectionManager(new ConsoleLogger(), EventStoreSettings.ClusterDns, EventStoreSettings.ExternalHttpPort);
            const string onEventReceivedStatementTemplate = "'{0}': onEventReceived";
            const string projectionDefinitionTemplate =
                @"var onEventReceived = function(s, e) {{ " +
                    "linkTo('{0}', e); " +
                "}};" +
                "fromAll().when({{ {1} }});";
            var onEventReceivedStatements = string.Join(",", _eventTypes.Select(eventType => string.Format(onEventReceivedStatementTemplate, eventType)));
            var projectionDefinition = string.Format(projectionDefinitionTemplate, "application1", onEventReceivedStatements);
            projectionManager.CreateOrUpdateProjectionAsync("application1", projectionDefinition, EventStoreSettings.Credentials, int.MaxValue).Wait();

            var consumerGroupManager = new ConsumerGroupManager(
                new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).KeepReconnecting()));
            consumerGroupManager.EnsureConsumerAsync(EventStoreSettings.Credentials, _subscriptionStreamName, _subscriptionGroupName).Wait();

            var persistentSubscription = new PersistentSubscription(
                new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
                    _subscriptionStreamName, _subscriptionGroupName,
                async e =>
                {
                    Console.WriteLine($"persistent subscription processed stream: {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId}");
                    await Task.Delay(500);
                    return await Task.FromResult(true);
                }, 1000, new ConsoleLogger());
            persistentSubscription.StartAsync().Wait();

            //var catchUpSubscription = new CatchUpSubscription(
            //    new EventStoreConnectionFactory(x => x.SetDefaultUserCredentials(EventStoreSettings.Credentials).UseConsoleLogger().KeepReconnecting()),
            //    "application1",
            //    e =>
            //    {
            //        Console.WriteLine($"catchup subscription processed stream: {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId} | thread : {Thread.CurrentThread.GetHashCode()}");
            //        return Task.FromResult(true);
            //    }, 1000, () => Task.FromResult(default(int?)), new ConsoleLogger());
            //catchUpSubscription.StartAsync().Wait();

            while (true);
        }

        
    }
}
