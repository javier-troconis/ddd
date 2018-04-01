using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class StartIdentityVerificationCommand
    {
        public struct VerifyApplicantIdentityResult
        {
            public VerifyApplicantIdentityResult(string transactionId, string result)
            {
                TransactionId = transactionId;
                Result = result;
            }

            public string TransactionId { get; }
            public string Result { get; }
        }

        public delegate Task<VerifyApplicantIdentityResult> VerifyApplicantIdentity(Ssn applicantSsn);

        public struct StartIdentityVerificationCommandContext : IMessageHandler<IRecordedEvent<IApplicationStartedV1>, StartIdentityVerificationCommandContext>
        {
            public readonly Ssn ApplicantSsn;

            public StartIdentityVerificationCommandContext(Ssn applicantSsn)
            {
                ApplicantSsn = applicantSsn;
            }

            public StartIdentityVerificationCommandContext Handle(IRecordedEvent<IApplicationStartedV1> message)
            {
                return new StartIdentityVerificationCommandContext(message.Data.ApplicantSsn);
            }
        }

        public struct IdentityVerificationCompleted : IIdentityVerificationCompletedV1
        {
            public IdentityVerificationCompleted(string transactionId, string result)
            {
                TransactionId = transactionId;
                Result = result;
            }

            public string TransactionId { get; }
            public string Result { get; }
        }

        public static async Task<IdentityVerificationCompleted> StartIdentityVerification(StartIdentityVerificationCommandContext commandContext, VerifyApplicantIdentity verifyApplicantIdentity)
        {
            var result = await verifyApplicantIdentity(commandContext.ApplicantSsn);
            return new IdentityVerificationCompleted(result.TransactionId, result.Result);
        }
    }
}
