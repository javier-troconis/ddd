using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface ISubscriptionStreamProvisioningController
		: IMessageHandler<IRecordedEvent<IProvisionSubscriptionStream>, Task>,
		  IMessageHandler<IRecordedEvent<IProvisionAllSubscriptionStreams>, Task>
	{

	}
}
