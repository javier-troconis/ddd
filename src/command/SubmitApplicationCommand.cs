using command.contracts;
using shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace command
{
    public static class SubmitApplicationCommand
    {
        public struct SubmitApplicationCommandContext : IMessageHandler<IRecordedEvent<IIdentityVerificationCompletedV1>, SubmitApplicationCommandContext>
        {
            public readonly bool IsIdentityVerificationCompleted;

            public SubmitApplicationCommandContext(bool isIdentityVerificationCompleted)
            {
                IsIdentityVerificationCompleted = isIdentityVerificationCompleted;
            }

            public SubmitApplicationCommandContext Handle(IRecordedEvent<IIdentityVerificationCompletedV1> message)
            {
                return new SubmitApplicationCommandContext(true);
            }
        }

        public struct ApplicationSubmitted : IApplicationSubmittedV1
        {

        }

        public static ApplicationSubmitted SubmitApplication(SubmitApplicationCommandContext context)
        {
            if (!context.IsIdentityVerificationCompleted)
            {
                throw new Exception("identity verification has not been completed");
            }
            return new ApplicationSubmitted();
        }
    }
}
