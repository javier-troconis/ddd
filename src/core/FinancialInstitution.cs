using System;
using shared;

namespace core
{
	public class FinancialInstitutionRegistered : Event<FinancialInstitutionRegistered>
	{

	}

	public class FinancialInstitution : Aggregate
    {
	    public FinancialInstitution(Guid id) : base(id)
	    {
			
	    }

		public void Register()
		{
			RecordThat(new FinancialInstitutionRegistered());
		}
    }
}
