using command.contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class StartApplication
    {
        public struct ApplicationStarted : IApplicationStartedV1
        {
            public ApplicationStarted(string applicantSsn)
            {
                ApplicantSsn = applicantSsn;
            }

            public string ApplicantSsn { get; }
        }

        public static ApplicationStarted Execute(string applicantSsn)
        {
            return new ApplicationStarted(applicantSsn);
        }
    }
}
