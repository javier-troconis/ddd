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
        public struct VerifyIdentityResult
        {
            public VerifyIdentityResult(string transactionId, string result)
            {
                TransactionId = transactionId;
                Result = result;
            }

            public string TransactionId { get; }
            public string Result { get; }
        }

        public delegate Task<VerifyIdentityResult> VerifyIdentity(string ssn);

        public class StartIdentityVerificationCommandContext : IMessageHandler<IRecordedEvent<IApplicationStartedV1>, StartIdentityVerificationCommandContext>
        {
            public readonly string ApplicantSsn;

            public StartIdentityVerificationCommandContext(string applicantSsn)
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

        public static async Task<IdentityVerificationCompleted> StartIdentityVerification(StartIdentityVerificationCommandContext commandContext, VerifyIdentity verifyIdentity)
        {
            Ensure.NotNull(commandContext, nameof(commandContext));
            var result = await verifyIdentity(commandContext.ApplicantSsn);
            return new IdentityVerificationCompleted(result.TransactionId, result.Result);
        }
    }
}
