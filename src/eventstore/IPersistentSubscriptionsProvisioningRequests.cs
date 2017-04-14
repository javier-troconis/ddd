

using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface IPersistentSubscriptionsProvisioningRequests
		: IMessageHandler<IRecordedEvent<IPersistentSubscriptionsProvisioningRequested>, Task>
	{

	}
}
