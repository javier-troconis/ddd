

using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioningController
		: 
		IMessageHandler<IRecordedEvent<IProvisionPersistentSubscription>, Task>,
		IMessageHandler<IRecordedEvent<IProvisionAllPersistentSubscriptions>, Task>
	{

	}
}
