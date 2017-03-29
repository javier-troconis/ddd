namespace command.contracts
{
    public interface IApplicationStartedV1
    {
    }

	public interface IApplicationStartedV2 : IApplicationStartedV1
	{
	}

	public interface IApplicationStartedV3 : IApplicationStartedV2
	{
	}

	public interface IApplicationSubmittedV1
    {
		string SubmittedBy { get; }
    }

}
