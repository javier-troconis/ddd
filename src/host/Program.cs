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
		static IEventSourcedEntityRepository _eventSourcedEntityRepository;

		public static void Main(string[] args)
		{
			var eventStoreConnection = EventStoreConnection.Create(ConnectionSettings
				.Create()
				.SetDefaultUserCredentials(new UserCredentials("admin", "admin")),
					new IPEndPoint(Array.Find(Dns.GetHostEntry("localhost").AddressList, x => x.AddressFamily.Equals(AddressFamily.InterNetwork)), 1113));

			eventStoreConnection.ConnectAsync().Wait();

			_eventSourcedEntityRepository = new EventSourcedEntityRepository(new infra.EventStore(eventStoreConnection));

			Task.Run(async () =>
			{
				// application
				var applicationId = Guid.NewGuid();

				await StartApplication(applicationId);

				await SubmitApplication(applicationId);

				await SubmitApplication(applicationId);

				await SubmitApplication(applicationId);

				await PrintApplicationSubmittalCount(applicationId);

				// financial institution
				var financialInstitutionId = Guid.NewGuid();

				await RegisterFinancialInstitution(financialInstitutionId);

				await CreditFinancialInstitutionBankAccount(financialInstitutionId);

				await DeactivateFinancialInstitution(financialInstitutionId);

				await CreditFinancialInstitutionBankAccount(financialInstitutionId);
			}).Wait();
		}

		static async Task DeactivateFinancialInstitution(Guid entityId)
		{
			var entity = new FinancialInstitution(entityId);
			await _eventSourcedEntityRepository.Load(entityId, entity);
			entity.Deactivate();
			await _eventSourcedEntityRepository.Save(entity);
		}

		static async Task CreditFinancialInstitutionBankAccount(Guid entityId)
		{
			var entity = new FinancialInstitutionBankAccount(entityId);
			await _eventSourcedEntityRepository.Load(entityId, entity);
			try
			{
				entity.Credit(10);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}
			await _eventSourcedEntityRepository.Save(entity);
		}

	    static async Task RegisterFinancialInstitution(Guid entityId)
		{
			var entity = new FinancialInstitution(entityId);
			entity.Register();
			await _eventSourcedEntityRepository.Save(entity);
		}

		static async Task PrintApplicationSubmittalCount(Guid entityId)
		{
			var entity = new ApplicationSubmittalCounter();
			await _eventSourcedEntityRepository.Load(entityId, entity);
			Console.WriteLine($"application {entityId} has been submitted {entity.SubmittalCount} times");
		}

		static async Task SubmitApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			await _eventSourcedEntityRepository.Load(entityId, entity);
			entity.Submit();
			await _eventSourcedEntityRepository.Save(entity);
		}

		static async Task StartApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			entity.Start();
			await _eventSourcedEntityRepository.Save(entity);
		}
	}
}
