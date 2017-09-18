using shared;

namespace command
{
	public struct SubmitApplicationState :
		IMessageHandler<ApplicationStartedV1, SubmitApplicationState>,
		IMessageHandler<ApplicationStartedV2, SubmitApplicationState>,
		IMessageHandler<ApplicationStartedV3, SubmitApplicationState>
	{
		public readonly bool ApplicationHasBeenStarted;

		public SubmitApplicationState(bool applicationHasBeenStarted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
		}

		public SubmitApplicationState Handle(ApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(ApplicationStartedV2 message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(ApplicationStartedV3 message)
		{
			return new SubmitApplicationState(true);
		}
	}
}
