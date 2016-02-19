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
		static readonly IEventStreamRepository EventStreamRepository = new EventStreamRepository();
	
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
			EventStreamRepository.Load(entityId, entity);
			entity.Deactivate();
			EventStreamRepository.Save(entity);
		}

		private static void CreditFinancialInstitutionBankAccount(Guid entityId)
		{
			var entity = new FinancialInstitutionBankAccount(entityId);
			EventStreamRepository.Load(entityId, entity);
			entity.Credit(10);
			EventStreamRepository.Save(entity);
		}

		private static void RegisterFinancialInstitution(Guid entityId)
		{
			var entity = new FinancialInstitution(entityId);
			entity.Register();
			EventStreamRepository.Save(entity);
		}

		private static void PrintApplicationSubmittalCount(Guid entityId)
		{
			var entity = new ApplicationSubmittalCounter();
			EventStreamRepository.Load(entityId, entity);
			Console.WriteLine(entity.Submittals);
		}


		static void SubmitApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			EventStreamRepository.Load(entityId, entity);
			entity.Submit();
			EventStreamRepository.Save(entity);
		}

		static void StartApplication(Guid entityId)
		{
			var entity = new Application(entityId);
			entity.Start();
			EventStreamRepository.Save(entity);
		}
	}
}
