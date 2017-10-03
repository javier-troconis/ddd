using command.contracts;
using shared;

namespace command
{
	public struct SubmitApplicationState :
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, SubmitApplicationState>,
		IMessageHandler<IRecordedEvent<IApplicationStartedV2>, SubmitApplicationState>,
		IMessageHandler<IRecordedEvent<IApplicationStartedV3>, SubmitApplicationState>
	{
		public readonly bool ApplicationHasBeenStarted;

		public SubmitApplicationState(bool applicationHasBeenStarted)
		{
			ApplicationHasBeenStarted = applicationHasBeenStarted;
		}

		public SubmitApplicationState Handle(IRecordedEvent<IApplicationStartedV1> message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(IRecordedEvent<IApplicationStartedV2> message)
		{
			return new SubmitApplicationState(true);
		}

		public SubmitApplicationState Handle(IRecordedEvent<IApplicationStartedV3> message)
		{
			return new SubmitApplicationState(true);
		}
	}
}
