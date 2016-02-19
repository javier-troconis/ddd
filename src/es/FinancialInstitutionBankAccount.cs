using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public class FinancialInstitutionBankAccountState : IEventSourcedEntity<FinancialInstitutionDeactivated>
	{
		public void Apply(FinancialInstitutionDeactivated @event)
		{
			IsActive = false;
		}

		public bool IsActive { get; private set; } = true;
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
