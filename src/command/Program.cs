using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using EventStore.ClientAPI;

using eventstore;

using shared;

namespace command
{
	public class Program
	{
		public static void Main(string[] args)
		{
            /*
			while (true)
			{
				Console.WriteLine("1 - IApplicationStartedV1");
				Console.WriteLine("2 - IApplicationStartedV2");
				Console.WriteLine("3 - IApplicationStartedV3");

				var option = Console.ReadKey().KeyChar;
				switch (option)
				{
					case '1':
						eventStore.WriteEvents(
							 "application-" + Guid.NewGuid().ToString("N").ToLower(),
							 ExpectedVersion.NoStream,
							 Command.StartApplicationV1())
						 .Wait();
						break;
					case '2':
						eventStore.WriteEvents(
							 "application-" + Guid.NewGuid().ToString("N").ToLower(),
							 ExpectedVersion.NoStream,
							 Command.StartApplicationV2())
						 .Wait();
						break;
					case '3':
						eventStore.WriteEvents(
							"application-" + Guid.NewGuid().ToString("N").ToLower(),
							ExpectedVersion.NoStream,
							Command.StartApplicationV3())
							.Wait();
						break;
					default:
						return;
				}
				Console.WriteLine();
				Console.WriteLine();
			}
			*/

            //await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
            //Console.WriteLine("application submitted: " + streamName);


			var connectionFactory = new EventStoreConnectionFactory(
				EventStoreSettings.ClusterDns,
				EventStoreSettings.InternalHttpPort,
				EventStoreSettings.Username,
				EventStoreSettings.Password,
				x => x
					.WithConnectionTimeoutOf(TimeSpan.FromMinutes(1)));
			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
			IEventStore eventStore = new eventstore.EventStore(connection);

			while (true)
			{
				Task.Run(async () =>
				{
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
					await StartApplication(eventStore, streamName);
					await StartIdentityVerification(eventStore, streamName);
					await SubmitApplication(eventStore, streamName);
					await Task.Delay(5000);
				}).Wait();
			}
        }

		private static async Task StartApplication(IEventStore eventStore, string streamName)
		{
			var newEvent = StartApplicationCommand.StartApplication("123456789");
			await eventStore.WriteEvent(streamName, ExpectedVersion.NoStream, newEvent);
			Console.WriteLine("application started: " + streamName);
		}

		private static async Task StartIdentityVerification(IEventStore eventStore, string streamName)
		{
			var events = await eventStore.ReadEventsForward(streamName);
			var context = events.Aggregate<StartIdentityVerificationCommand.StartIdentityVerificationCommandContext>();
			Task<StartIdentityVerificationCommand.VerifyIdentityResult> VerifyIdentity(Ssn ssn) => Task.FromResult(new StartIdentityVerificationCommand.VerifyIdentityResult(Guid.NewGuid().ToString("N"), "passed"));
			var newEvent = await StartIdentityVerificationCommand.StartIdentityVerification(context, VerifyIdentity);
			await eventStore.WriteEvent(streamName, 0, newEvent);
			Console.WriteLine("identity verification completed: " + streamName);
		}

		private static async Task SubmitApplication(IEventStore eventStore, string streamName)
		{
			var events = await eventStore.ReadEventsForward(streamName);
			var context = events.Aggregate<SubmitApplicationCommand.SubmitApplicationCommandContext>();
			var newEvent = SubmitApplicationCommand.SubmitApplication(context);
			await eventStore.WriteEvent(streamName, 1, newEvent);
			Console.WriteLine("application submitted: " + streamName);
		}
	}
}
