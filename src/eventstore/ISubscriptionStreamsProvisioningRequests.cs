using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface ISubscriptionStreamsProvisioningRequests
		: IMessageHandler<IRecordedEvent<ISubscriptionStreamsProvisioningRequested>, Task>
	{

	}
}
