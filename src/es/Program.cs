using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace es
{
	
	public class Program
	{
		static readonly IEventSourcedEntityRepository EventSourcedEntityRepository = new EventSourcedEntityRepository();
	
		public static void Main(string[] args)
		{
			// application
			var applicationId = Guid.NewGuid();

			StartApplication(applicationId);

			SubmitApplication(applicationId);

			SubmitApplication(applicationId);

			SubmitApplication(applicationId);

			PrintApplicationSubmittalCount(applicationId);

			// financial institution
			var financialInstitutionId = Guid.NewGuid();

			RegisterFinancialInstitution(financialInstitutionId);

			CreditFinancialInstitutionBankAccount(financialInstitutionId);

			DeactivateFinancialInstitution(financialInstitutionId);

			CreditFinancialInstitutionBankAccount(financialInstitutionId);
		}

		private static void DeactivateFinancialInstitution(Guid entityId)
		{
			var entity = new FinancialInstitution(entityId);
			EventSourcedEntityRepository.Load(entityId, entity);
			entity.Deactivate();
			EventSourcedEntityRepository.Save(entity);
		}

		private static void CreditFinancialInstitutionBankAccount(Guid entityId)
		{
			var entity = new FinancialInstitutionBankAccount(entityId);
			EventSourcedEntityRepository.Load(entityId, entity);
			try
			{
				entity.Credit(10);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.Message);
				return;
			}
			EventSourcedEntityRepository.Save(entity);
		}

		private static void RegisterFinancialInstitution(Guid entityId)
		{
			var entity = new FinancialInstitution(entityId);
			entity.Register();
			EventSourcedEntityRepository.Save(entity);
		}

		private static void PrintApplicationSubmittalCount(Guid entityId)
		{
			var entity = new ApplicationSubmittalCounter();
			EventSourcedEntityRepository.Load(entityId, entity);
			Console.WriteLine($"application {entityId} has been submitted {entity.SubmittalCount} times");
		}


		static void SubmitApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			EventSourcedEntityRepository.Load(entityId, entity);
			entity.Submit();
			EventSourcedEntityRepository.Save(entity);
		}

		static void StartApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			entity.Start();
			EventSourcedEntityRepository.Save(entity);
		}
	}
}
