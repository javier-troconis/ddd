using infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace subscriber
{
    public class Program
    {
        private static readonly IEventStoreConnection Connection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
        private const string SubscriptionGroupName = "application1";
        private static readonly string[] AvailableEventTypes = { "applicationsubmitted", "applicationstarted" };
        private static readonly ProjectionsManager ProjectionsManager = new ProjectionsManager(new ConsoleLogger(), EventStoreSettings.ExternalHttpEndPoint, TimeSpan.FromMilliseconds(5000));

        static Program()
        {
            Connection.Disconnected += (s, a) =>
            {
                Console.WriteLine("disconnected");
            };

            Connection.Closed += (s, a) =>
            {
                Console.WriteLine("closed");
            };

            Connection.Connected += (s, a) =>
            {
                Console.WriteLine("connected");
            };

            Connection.Reconnecting += (s, a) =>
            {
                Console.WriteLine("reconnecting");
            };

            Connection.ConnectAsync().Wait();
        }

        public static void Main(string[] args)
        {
            CreateSubscriberGroup().Wait();

            Subscribe().Wait();

            while (true)
            {
                //Console.ReadLine();
                //Task.Delay(2000)
                //	.ContinueWith(delegate
                //	{
                //		CreateSubscriberGroup();
                //	}).Wait();
            }
        }

        public static async Task CreateSubscriberGroup()
        {
            //var eventTypes = AvailableEventTypes.Take(Random.Next(1, AvailableEventTypes.Length + 1)).ToArray();
            var eventTypes = AvailableEventTypes;

            Console.WriteLine("listening to: " + string.Join(", ", eventTypes));

            const string onEventReceivedStatementTemplate = "'{0}': onEventReceived";
            const string projectionDefinitionTemplate =
                @"var onEventReceived = function(s, e) {{ " +
                    "linkTo('{0}', e); " +
                "}};" +
                "fromAll().when({{ {1} }});";

            var onEventReceivedStatements = string.Join(",", eventTypes.Select(eventType => string.Format(onEventReceivedStatementTemplate, eventType)));

            //var projectionDestinationStream = $"{SubscriptionGroupName}-{Guid.NewGuid().ToString("N").ToLower()}";

            //Connection.AppendToStreamAsync("projectionsink", ExpectedVersion.Any, new EventData(Guid.NewGuid(), SubscriptionGroupName, false, 
            //	Encoding.UTF8.GetBytes($"{{'destinationStream' : '{projectionDestinationStream}'}}"), new byte[0]));

            //var projectionDefinition = string.Format(projectionDefinitionTemplate, projectionDestinationStream, onEventReceivedStatements);




            var projectionDefinition = string.Format(projectionDefinitionTemplate, SubscriptionGroupName, onEventReceivedStatements);

            CreateOrUpdateProjection(SubscriptionGroupName, projectionDefinition).Wait();

            var subcriptionGroupSettingsBuilder = PersistentSubscriptionSettings.Create()
                    .ResolveLinkTos()
                    .WithMaxRetriesOf(2)
                    .MinimumCheckPointCountOf(2)
                    .StartFromCurrent();
            var subcriptionGroupSettings = subcriptionGroupSettingsBuilder.Build();

            try
            {
                await Connection.CreatePersistentSubscriptionAsync(SubscriptionGroupName, SubscriptionGroupName, subcriptionGroupSettings, EventStoreSettings.Credentials);
            }
            catch (InvalidOperationException)
            {
                
            }
        }

        public static async Task Subscribe()
        {
            await Connection.ConnectToPersistentSubscriptionAsync(SubscriptionGroupName, SubscriptionGroupName,
                (s, e) =>
                    {
                        try
                        {
                            Console.WriteLine($"stream: {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId}");
                            s.Acknowledge(e);
                        }
                        catch(Exception ex)
                        {
                            s.Fail(e, PersistentSubscriptionNakEventAction.Unknown, ex.Message);
                        }
                        
                    },
                async (s, r, e) =>
                    {
                        await Subscribe();
                        Console.WriteLine("subscription dropped");
                    }, autoAck: false);
        }

        private static async Task CreateOrUpdateProjection(string projectionName, string projectionDefinition)
        {
            if (await IsProjectionNew(projectionName))
            {
                await CreateProjection(projectionName, projectionDefinition);
            }
            else if (await HasProjectionChanged(projectionName, projectionDefinition))
            {
                await UpdateProjection(projectionName, projectionDefinition);
            }
        }

        private static async Task<bool> IsProjectionNew(string projectionName)
        {
            try
            {
                await ProjectionsManager.GetQueryAsync(projectionName, EventStoreSettings.Credentials);
                return false;
            }
            catch (ProjectionCommandFailedException ex)
            {
                if (ex.HttpStatusCode != (int)HttpStatusCode.NotFound)
                {
                    throw;
                }
                return true;
            }
        }

        private static Task CreateProjection(string projectionName, string projectionDefinition)
        {
            return ProjectionsManager.CreateContinuousAsync(projectionName, projectionDefinition, EventStoreSettings.Credentials);
        }

        private static async Task<bool> HasProjectionChanged(string projectionName, string projectionDefinition)
        {
            var storedProjectionDefinition = await ProjectionsManager.GetQueryAsync(projectionName, EventStoreSettings.Credentials);
            return storedProjectionDefinition != projectionDefinition;
        }

        private static Task UpdateProjection(string projectionName, string projectionDefinition)
        {
            return ProjectionsManager.UpdateQueryAsync(projectionName, projectionDefinition, EventStoreSettings.Credentials);
        }
    }
}
