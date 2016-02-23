using System;
using shared;

namespace core
{
	
	public class FinancialInstitutionCredited : Event<FinancialInstitutionCredited>
	{
		public FinancialInstitutionCredited(decimal amount)
		{
			Amount = amount;
		}

		public decimal Amount { get; }
	}

	public class FinancialInstitutionLedgerState : IEventConsumer<FinancialInstitutionCredited>
	{
		public void Apply(FinancialInstitutionCredited @event)
		{
			CurrentBalance += @event.Amount;
		}

		public decimal CurrentBalance { get; private set; }
	}

	public class FinancialInstitutionLedger : AggregateRoot<FinancialInstitutionLedgerState>
    {
		private const decimal MaximumAllowedBalance = 30;

	    public FinancialInstitutionLedger(Guid id) : base(id)
	    {
			
	    }

		public void Credit(decimal amount)
		{
			if(State.CurrentBalance + amount > MaximumAllowedBalance)
			{
				throw new Exception($"cannot credit financial institution {Id} because the maximum allowed would be exceeded");
			}
			RecordThat(new FinancialInstitutionCredited(amount));
		}
    }
}
