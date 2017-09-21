using command.contracts;
using shared;

namespace command
{
	public struct SubmitApplicationState :
		IMessageHandler<IApplicationStartedV1, SubmitApplicationState>,
		IMessageHandler<IApplicationStartedV2, SubmitApplicationState>,
		IMessageHandler<IApplicationStartedV3, SubmitApplicationState>
	{
		public readonly bool ApplicationHasBeenStarted;

		public SubmitApplicationState(bool applicationHasBeenStarted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
		}

		public SubmitApplicationState Handle(IApplicationStartedV1 message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(IApplicationStartedV2 message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(IApplicationStartedV3 message)
		{
			return new SubmitApplicationState(true);
		}
	}
}
