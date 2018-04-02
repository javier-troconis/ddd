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

			while (true)
			{
				Task.Run(async () =>
				{
					var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
					await eventStore.WriteEvent(streamName, ExpectedVersion.NoStream, StartApplicationCommand.StartApplication("123456789"));
					Console.WriteLine("application started: " + streamName);

					var events = await eventStore.ReadEventsForward(streamName);
					var fnc = SubscriberResolvedEventHandleFactory.CreateSubscriberResolvedEventHandle<StartIdentityVerificationCommand.StartIdentityVerificationCommandContext, StartIdentityVerificationCommand.StartIdentityVerificationCommandContext>((result, resolvedEvent) => result);
					var commandContext = events.Aggregate(new StartIdentityVerificationCommand.StartIdentityVerificationCommandContext(), fnc);
					var newEvent = await StartIdentityVerificationCommand.StartIdentityVerification(commandContext, ssn => Task.FromResult(new StartIdentityVerificationCommand.VerifyIdentityResult(Guid.NewGuid().ToString("N"), "passed")));
					await eventStore.WriteEvent(streamName, 0, newEvent);
					Console.WriteLine("identity verification completed: " + streamName);

					//await OptimisticEventWriter.WriteEvents(eventStore, streamName, ExpectedVersion.NoStream, newEvents, ConflictResolutionStrategy.IgnoreConflicts);
					//Console.WriteLine("application submitted: " + streamName);

					await Task.Delay(2000);
				}).Wait();
			}
		}
	}
}
