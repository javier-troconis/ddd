using System;
using shared;

namespace core
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
				throw new Exception($"financial institution {Id} cannot be credited because is inactive");
			}
			RecordThat(new FinancialInstitutionBankAccountCredited());
		}
    }
}
