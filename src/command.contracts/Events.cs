namespace command.contracts
{
    public interface IApplicationStartedV1
    {
        string ApplicantSsn { get; }
    }

    public interface IIdentityVerificationCompletedV1
    {
        string TransactionId { get; }
        string Result { get; }
    }

    public interface IApplicationSubmittedV1
    {	
    }
}
