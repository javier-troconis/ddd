using command.contracts;

namespace command
{
	public struct ApplicationStartedV1 : IApplicationStartedV1
	{

	}

	public struct ApplicationStartedV2 : IApplicationStartedV2, IApplicationStartedV1
	{

	}

	public struct ApplicationStartedV3 : IApplicationStartedV3, IApplicationStartedV2, IApplicationStartedV1
	{

	}

	public struct ApplicationSubmittedV1 : IApplicationSubmittedV1
	{
		public ApplicationSubmittedV1(string submittedBy)
		{
			SubmittedBy = submittedBy;
		}

		public string SubmittedBy { get; }
	}
}
