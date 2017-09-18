

namespace eventstore
{
	public interface IStreamMetadataChangeRequested
    {
		string StreamName { get; }
	}

	internal class StreamMetadataChangeRequested : IStreamMetadataChangeRequested
    {
	    public StreamMetadataChangeRequested(string streamName)
	    {
            StreamName = streamName;
	    }

	    public string StreamName { get; }
	}
}
