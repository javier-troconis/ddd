using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
	public static class StartApplicationCommand_V1
	{
		public struct ApplicationStarted :
			IApplicationStarted_V1
		{
			public ApplicationStarted(Ssn applicantSsn)
			{
				ApplicantSsn = applicantSsn;
			}

			public string ApplicantSsn { get; }
		}

		public static ApplicationStarted StartApplication(Ssn applicantSsn)
		{
			Ensure.NotDefault(applicantSsn, nameof(applicantSsn));
			return new ApplicationStarted(applicantSsn);
		}
	}

	public static class StartApplicationCommand_V2
	{
		public struct ApplicationStarted :
			IApplicationStarted_V2,
			IApplicationStarted_V1
		{
			public ApplicationStarted(Ssn applicantSsn, string applicantEmail)
			{
				ApplicantSsn = applicantSsn;
				ApplicantEmail = applicantEmail;
			}

			public string ApplicantSsn { get; }
			public string ApplicantEmail { get; set; }
		}

		public static ApplicationStarted StartApplication(Ssn applicantSsn, string applicantEmail)
		{
			Ensure.NotDefault(applicantSsn, nameof(applicantSsn));
			Ensure.NotDefault(applicantEmail, nameof(applicantEmail));
			return new ApplicationStarted(applicantSsn, applicantEmail);
		}
	}

	public static class StartApplicationCommand_V3
    {
        public struct ApplicationStarted : 
	        IApplicationStarted_V3, 
	        IApplicationStarted_V2, 
	        IApplicationStarted_V1
		{
            public ApplicationStarted(Ssn applicantSsn, string applicantEmail, string applicantName)
            {
	            ApplicantSsn = applicantSsn;
	            ApplicantEmail = applicantEmail;
	            ApplicantName = applicantName;
            }

            public string ApplicantSsn { get; }
			public string ApplicantEmail { get; set; }
			public string ApplicantName { get; set; }
		}

        public static ApplicationStarted StartApplication(Ssn applicantSsn, string applicantEmail, string applicantName)
        {
            Ensure.NotDefault(applicantSsn, nameof(applicantSsn));
	        Ensure.NotDefault(applicantEmail, nameof(applicantEmail));
	        Ensure.NotDefault(applicantName, nameof(applicantName));
			return new ApplicationStarted(applicantSsn, applicantEmail, applicantName);
        }
    }
}
