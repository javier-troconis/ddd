using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using shared;

namespace core
{

    public struct ApplicationSubmitted : IEvent
    {
        public readonly string SubmittedBy;

        public ApplicationSubmitted(string submittedBy)
        {
            SubmittedBy = submittedBy;
        }
    }

    public struct WhenSubmittingApplicationState :
        IMessageHandler<ApplicationStarted, WhenSubmittingApplicationState>,
        IMessageHandler<ApplicationSubmitted, WhenSubmittingApplicationState>
    {
        public readonly bool HasBeenStarted;
        public readonly bool HasBeenSubmitted;

        public WhenSubmittingApplicationState(bool hasBeenStarted, bool hasBeenSubmitted)
        {
            HasBeenStarted = hasBeenStarted;
            HasBeenSubmitted = hasBeenSubmitted;
        }

        public WhenSubmittingApplicationState Handle(ApplicationSubmitted message)
        {
            return new WhenSubmittingApplicationState(HasBeenStarted, true);
        }

        public WhenSubmittingApplicationState Handle(ApplicationStarted message)
        {
            return new WhenSubmittingApplicationState(true, HasBeenSubmitted);
        }
    }

    public static class SubmitApplication
    {
        public static IEnumerable<IEvent> Apply(WhenSubmittingApplicationState state, string submitter)
        {
            Ensure(state);
            yield return new ApplicationSubmitted(submitter);
        }

        private static void Ensure(WhenSubmittingApplicationState state)
        {
            if (!state.HasBeenStarted)
            {
                throw new Exception("application has not been started");
            }
            if (state.HasBeenSubmitted)
            {
                throw new Exception("application has already been submitted");
            }
        }
    }
}
