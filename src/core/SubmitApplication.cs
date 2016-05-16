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
        public static IEnumerable<IEvent> Execute(WhenSubmittingApplicationState state, string submitter)
        {
            if (!state.HasBeenStarted)
            {
                throw new Exception("application has not been started");
            }
            if (state.HasBeenSubmitted)
            {
                throw new Exception("application has already been submitted");
            }
            return DoExecute(state, submitter);
        }

        private static IEnumerable<IEvent> DoExecute(WhenSubmittingApplicationState state, string submitter)
        {
            yield return new ApplicationSubmitted(submitter);
        }
    }
}
