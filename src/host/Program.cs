using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
		static readonly IEventSourcedEntityRepositoryFactory EventSourcedEntityRepositoryFactory;

		static Program()
		{
			var eventStoreConnection = EventStoreConnection.Create(ConnectionSettings
				.Create().SetDefaultUserCredentials(new UserCredentials("admin", "admin")), new Uri("tcp://127.0.0.1:1113"));
			eventStoreConnection.ConnectAsync().Wait();
			EventSourcedEntityRepositoryFactory = new EventSourcedEntityRepositoryFactory(new infra.EventStore(eventStoreConnection));
		}

		public static void Main(string[] args)
		{
			var applicationId = Guid.NewGuid();
			var financialInstitutionId = Guid.NewGuid();

			RunSequence
			(

				StartApplication(applicationId),
				SubmitApplication(applicationId),
				SubmitApplication(applicationId),
				SubmitApplication(applicationId),
				PrintApplicationSubmittalCount(applicationId),

				RegisterFinancialInstitution(financialInstitutionId),
				RegisterFinancialInstitution(financialInstitutionId),
				CreditFinancialInstitution(financialInstitutionId),
				CreditFinancialInstitution(financialInstitutionId),
				CreditFinancialInstitution(financialInstitutionId),
				CreditFinancialInstitution(Guid.NewGuid())

			).Wait();
		}

		static Func<Task> RegisterFinancialInstitution(Guid entityId)
		{
			return async () =>
			{
				var entity = new FinancialInstitutionRegistration(entityId);
				var repository = EventSourcedEntityRepositoryFactory.CreateForStreamCategory("financial_institution");
				entity.Register();
				await repository.Save(entity);
			};
		}

		static Func<Task> CreditFinancialInstitution(Guid entityId)
		{
			return async () =>
			{
				var entity = new FinancialInstitutionLedger(entityId);
				var repository = EventSourcedEntityRepositoryFactory.CreateForStreamCategory("");
				await repository.Load(entityId, entity);
				entity.Credit(15);
				await repository.Save(entity);
			};
		}

		static Func<Task> StartApplication(Guid entityId)
		{
			return async () =>
			{
				var entity = new Application(entityId);
				var repository = EventSourcedEntityRepositoryFactory.CreateForStreamCategory("application");
				entity.Start();
				await repository.Save(entity);
			};
		}

		static Func<Task> SubmitApplication(Guid entityId)
		{
			return async () =>
			{
				var entity = new Application(entityId);
				var repository = EventSourcedEntityRepositoryFactory.CreateForStreamCategory("application");
				await repository.Load(entityId, entity);
				entity.Submit();
				await repository.Save(entity);
			};
		}

		static Func<Task> PrintApplicationSubmittalCount(Guid entityId)
		{
			return async () =>
			{
				var entity = new ApplicationSubmittalCount();
				var repository = EventSourcedEntityRepositoryFactory.CreateForStreamCategory("application");
				await repository.Load(entityId, entity);
				Console.WriteLine($"application {entityId} has been submitted {entity.SubmittalCount} times");
			};
		}

		static async Task RunSequence(params Func<Task>[] actions)
		{
			foreach (var action in actions)
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

	}
}
