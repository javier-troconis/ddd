using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class SubmitApplication
    {
        public struct ApplicationSubmitted : IApplicationSubmittedV1
        {
            
        }

        public struct State : IMessageHandler<IRecordedEvent<IIdentityVerificationCompletedV1>, State>
        {
            public readonly bool IsIdentityVerificationCompleted;

            public State(bool isIdentityVerificationCompleted)
            {
                IsIdentityVerificationCompleted = isIdentityVerificationCompleted;
            }

            public State Handle(IRecordedEvent<IIdentityVerificationCompletedV1> message)
            {
                return new State(true);
            }
        }

        public static ApplicationSubmitted Execute(State state)
        {
            if (!state.IsIdentityVerificationCompleted)
            {
                throw new Exception("identity verification has not been completed");
            }
            return new ApplicationSubmitted();
        }
    }
}
