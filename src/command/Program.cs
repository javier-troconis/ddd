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


			Task.Run(async () =>
			{
				while (true)
				{
					Console.WriteLine("1 - start application V1");
					Console.WriteLine("2 - start application V2");
					Console.WriteLine("3 - start application V3");
					var option = Console.ReadKey().KeyChar;
					Console.WriteLine();
					switch (option)
					{
						case '1':
							await StartApplication_V1(eventStore, "application-" + Guid.NewGuid().ToString("N").ToLower());
							break;
						case '2':
							await StartApplication_V2(eventStore, "application-" + Guid.NewGuid().ToString("N").ToLower());
							break;
						case '3':
							await StartApplication_V3(eventStore, "application-" + Guid.NewGuid().ToString("N").ToLower());
							break;
						default:
							return;
					}
				}
			}).Wait();

			//Task.Run(async () =>
			//{
			//	while (true)
			//	{
			//		var streamName = "application-" + Guid.NewGuid().ToString("N").ToLower();
			//		await StartApplication_V3(eventStore, streamName);
			//		await StartIdentityVerification(eventStore, streamName);
			//		await SubmitApplication(eventStore, streamName);
			//		await Task.Delay(5000);
			//	}
			//}).Wait();
			
		}

		private static async Task StartApplication_V1(IEventStore eventStore, string streamName)
		{
			var newEvent = StartApplicationCommand_V1.StartApplication("123456789");
			await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, new[] { newEvent });
			Console.WriteLine("application started: " + streamName);
		}

		private static async Task StartApplication_V2(IEventStore eventStore, string streamName)
		{
			var newEvent = StartApplicationCommand_V2.StartApplication("123456789", "jane@jane.com");
			await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, new[] { newEvent });
			Console.WriteLine("application started: " + streamName);
		}

		private static async Task StartApplication_V3(IEventStore eventStore, string streamName)
		{
			var newEvent = StartApplicationCommand_V3.StartApplication("123456789", "jane@jane.com", "jane");
			await eventStore.WriteEvents(streamName, ExpectedVersion.NoStream, new[] { newEvent });
			Console.WriteLine("application started: " + streamName);
		}

		private static async Task StartIdentityVerification(IEventStore eventStore, string streamName)
		{
			var history = await eventStore.ReadEventsForward(streamName);
			var context = history.Aggregate<StartIdentityVerificationCommand.StartIdentityVerificationCommandContext>();
			Task<StartIdentityVerificationCommand.VerifyIdentityResult> VerifyIdentity(Ssn ssn) => 
				Task.FromResult(new StartIdentityVerificationCommand.VerifyIdentityResult(Guid.NewGuid().ToString("N"), "passed"));
			var newEvent = await StartIdentityVerificationCommand.StartIdentityVerification(context, VerifyIdentity);
			await eventStore.WriteEvents(streamName, 0, new[] { newEvent });
			Console.WriteLine("identity verification completed: " + streamName);
		}

		private static async Task SubmitApplication(IEventStore eventStore, string streamName)
		{
			var history = await eventStore.ReadEventsForward(streamName);
			var context = history.Aggregate<SubmitApplicationCommand.SubmitApplicationCommandContext>();
			var newEvent = SubmitApplicationCommand.SubmitApplication(context);
			await eventStore.WriteEvents(streamName, 1, new[] { newEvent });
			Console.WriteLine("application submitted: " + streamName);
		}
	}
}
