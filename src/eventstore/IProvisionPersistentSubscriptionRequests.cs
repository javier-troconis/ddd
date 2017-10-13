

using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface IProvisionPersistentSubscriptionRequests
		: IMessageHandler<IRecordedEvent<IProvisionPersistentSubscriptionRequested>, Task>
	{

	}
}
