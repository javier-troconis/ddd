using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace es
{
	public class FinancialInstitutionRegistered : Event<FinancialInstitutionRegistered>
	{

	}

	public class FinancialInstitutionDeactivated : Event<FinancialInstitutionDeactivated>
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

		public void Deactivate()
		{
			RecordThat(new FinancialInstitutionDeactivated());
		}
    }
}
