using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class StartApplicationCommand
    {
        public struct ApplicationStarted : IApplicationStartedV1
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
}
