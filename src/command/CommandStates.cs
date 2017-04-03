using shared;

namespace command
{
	public struct SubmitApplicationV1State :
		 IMessageHandler<ApplicationStartedV1, SubmitApplicationV1State>,
		 IMessageHandler<ApplicationStartedV2, SubmitApplicationV1State>
	{
		public readonly bool ApplicationHasBeenStarted;

		public SubmitApplicationV1State(bool applicationHasBeenStarted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
		}

		public SubmitApplicationV1State Handle(ApplicationStartedV1 message)
		{
			return new SubmitApplicationV1State(true);
		}

		public SubmitApplicationV1State Handle(ApplicationStartedV2 message)
		{
			return new SubmitApplicationV1State(true);
		}
	}
}
