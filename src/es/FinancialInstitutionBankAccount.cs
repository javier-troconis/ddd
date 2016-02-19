using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public class FinancialInstitutionBankAccountState : IEventConsumer<FinancialInstitutionRegistered>, IEventConsumer<FinancialInstitutionDeactivated>
	{
		public void Apply(FinancialInstitutionDeactivated @event)
		{
			IsActive = false;
		}

		public void Apply(FinancialInstitutionRegistered @event)
		{
			IsActive = true;
		}

		public bool IsActive { get; private set; }
	}

	public class FinancialInstitutionBankAccountCredited : Event<FinancialInstitutionBankAccountCredited>
	{

	}


	public class FinancialInstitutionBankAccount : Aggregate<FinancialInstitutionBankAccountState>
    {
		public FinancialInstitutionBankAccount(Guid id) : base(id)
		{

		}

		public void Credit(decimal amount)
		{
			if (!State.IsActive)
			{
				throw new Exception();
			}
			RecordThat(new FinancialInstitutionBankAccountCredited());
		}
    }
}
