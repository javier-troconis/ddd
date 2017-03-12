using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using contracts;
using shared;

namespace core
{

    public struct ApplicationSubmittedV1 : IApplicationSubmittedV1
    {
        public readonly string SubmittedBy;

        public ApplicationSubmittedV1(string submittedBy)
        {
            SubmittedBy = submittedBy;
        }
    }

    public struct WhenSubmittingApplicationState :
        IMessageHandler<ApplicationStartedV1, WhenSubmittingApplicationState>,
        IMessageHandler<ApplicationSubmittedV1, WhenSubmittingApplicationState>
    {
        public readonly bool HasBeenStarted;
        public readonly bool HasBeenSubmitted;

        public WhenSubmittingApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
        {
            HasBeenStarted = hasBeenStarted;
            HasBeenSubmitted = hasBeenSubmitted;
        }

        public WhenSubmittingApplicationState Handle(ApplicationSubmittedV1 message)
        {
            return new WhenSubmittingApplicationState(HasBeenStarted, true);
        }

        public WhenSubmittingApplicationState Handle(ApplicationStartedV1 message)
        {
            return new WhenSubmittingApplicationState(true, HasBeenSubmitted);
        }
    }

    public static class SubmitApplication
    {
        //todo: fix this
        public static IEnumerable<IEvent> Apply(WhenSubmittingApplicationState state, string submitter)
        {
            if (!state.HasBeenStarted)
            {
                throw new Exception("application has not been started");
            }
            if (state.HasBeenSubmitted)
            {
                throw new Exception("application has already been submitted");
            }

            yield return new ApplicationSubmittedV1(submitter);
        }
    }
}
