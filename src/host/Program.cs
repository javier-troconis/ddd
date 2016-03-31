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
		static readonly IEventSourcedEntityRepository EventSourcedEntityRepository;

		static Program()
		{
			var eventStoreConnection = EventStoreConnection.Create(ConnectionSettings
				.Create().SetDefaultUserCredentials(new UserCredentials("admin", "changeit")), new Uri("tcp://127.0.0.1:1113"));
			eventStoreConnection.ConnectAsync().Wait();
			EventSourcedEntityRepository = new EventSourcedEntityRepository(new infra.EventStore(eventStoreConnection));
		}

		public static void Main(string[] args)
		{
			//var applicationId = Guid.NewGuid();
			//var financialInstitutionId = Guid.NewGuid();

			//RunSequence
			//(
			//	StartApplication(applicationId),
			//	SubmitApplication(applicationId),
			//	SubmitApplication(applicationId),
			//	SubmitApplication(applicationId),
			//	PrintApplicationSubmittalCount(applicationId),

			//	RegisterFinancialInstitution(financialInstitutionId),
			//	RegisterFinancialInstitution(financialInstitutionId),
			//	CreditFinancialInstitution(financialInstitutionId),
			//	CreditFinancialInstitution(financialInstitutionId),
			//	CreditFinancialInstitution(financialInstitutionId),
			//	CreditFinancialInstitution(Guid.NewGuid())
			//).Wait();
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

		static Func<Task> RegisterFinancialInstitution(Guid entityId)
		{
			return async () =>
			{
				var entity = new FinancialInstitutionRegistration(entityId);
				entity.Register();
				await EventSourcedEntityRepository.Save(entity);
			};
		}

		static Func<Task> CreditFinancialInstitution(Guid entityId)
		{
			return async () =>
			{
				var entity = new FinancialInstitutionLedger(entityId);
				await EventSourcedEntityRepository.Load(entityId, entity);
				entity.Credit(15);
				await EventSourcedEntityRepository.Save(entity);
			};
		}

		static Func<Task> StartApplication(Guid entityId)
		{
			return async () =>
			{
				var entity = new Application(entityId);
				entity.Start();
				await EventSourcedEntityRepository.Save(entity);
			};
		}

		static Func<Task> SubmitApplication(Guid entityId)
		{
			return async () =>
			{
				var entity = new Application(entityId);
				await EventSourcedEntityRepository.Load(entityId, entity);
				entity.Submit();
				await EventSourcedEntityRepository.Save(entity);
			};
		}

		static Func<Task> PrintApplicationSubmittalCount(Guid entityId)
		{
			return async () =>
			{
				var entity = new ApplicationSubmittalCount();
				await EventSourcedEntityRepository.Load(entityId, entity);
				Console.WriteLine($"application {entityId} has been submitted {entity.SubmittalCount} times");
			};
		}

	}
}
