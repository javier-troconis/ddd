using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using core;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using infra;
using shared;

namespace host
{
	public class Program
	{
		private static readonly IEventStore EventStore;

		static Program()
		{
			var eventStoreConnection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
			eventStoreConnection.ConnectAsync().Wait();
			EventStore = new infra.EventStore(eventStoreConnection);
		}

		public static void Main(string[] args)
		{

			var applicationId = StreamNamingConvention.FromIdentity(Guid.NewGuid());
			//var financialInstitutionId = Guid.NewGuid();

			RunSequence
			(
				StartApplication(applicationId),
				SubmitApplication(applicationId, 0)
			//SubmitApplication(applicationId),
			//SubmitApplication(applicationId),
			//PrintApplicationSubmittalCount(applicationId),

			//RegisterFinancialInstitution(financialInstitutionId),
			//RegisterFinancialInstitution(financialInstitutionId),
			//CreditFinancialInstitution(financialInstitutionId),
			//CreditFinancialInstitution(financialInstitutionId),
			//CreditFinancialInstitution(financialInstitutionId),
			//CreditFinancialInstitution(Guid.NewGuid())
			).Wait();
		}

		static async Task RunSequence(params Func<Task>[] actions)
		{
			foreach(var action in actions)
			{
				try
				{
					await action();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			}
		}

		//static Func<Task> RegisterFinancialInstitution(Guid entityId)
		//{
		//	return async () =>
		//	{
		//		var entity = new FinancialInstitutionRegistration(entityId);
		//		entity.Register();
		//		await EventSourcedEntityRepository.Save(entity);
		//	};
		//}

		//static Func<Task> CreditFinancialInstitution(Guid entityId)
		//{
		//	return async () =>
		//	{
		//		var entity = new FinancialInstitutionLedger(entityId);
		//		await EventSourcedEntityRepository.Load(entityId, entity);
		//		entity.Credit(15);
		//		await EventSourcedEntityRepository.Save(entity);
		//	};
		//}

		static Func<Task> StartApplication(string applicationId)
		{
			return async () =>
			{
				var @event = new ApplicationStarted();
				await EventStore.SaveEventAsync(applicationId, @event);
			};
		}

		static Func<Task> SubmitApplication(string applicationId, int version)
		{
			return async () =>
			{
				var state = await StreamStateLoader.Load<ApplicationSubmitState>(EventStore, applicationId);
				var events = Application.Submit(state);
				await EventStore.SaveEventsAsync(applicationId, events, version);
			};
		}
	}
}
