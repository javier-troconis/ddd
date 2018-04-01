using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class StartIdentityVerification
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

        public struct State : IMessageHandler<IRecordedEvent<IApplicationStartedV1>, State>
        {
            public readonly string ApplicantSsn;

            public State(string applicantSsn)
            {
                ApplicantSsn = applicantSsn;
            }

            public State Handle(IRecordedEvent<IApplicationStartedV1> message)
            {
                return new State(message.Data.ApplicantSsn);
            }
        }

        public static async Task<IdentityVerificationCompleted> Execute(State state, VerifyIdentity verifyIdentity)
        {
            Ensure.NotDefault(state, nameof(state));
            var result = await verifyIdentity(state.ApplicantSsn);
            return new IdentityVerificationCompleted(result.TransactionId, result.Result);
        }
    }
}
