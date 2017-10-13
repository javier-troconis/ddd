using System.Threading.Tasks;

using shared;

namespace eventstore
{
	public interface IProvisionSubscriptionStreamRequests
		: IMessageHandler<IRecordedEvent<IProvisionSubscriptionStreamRequested>, Task>
	{

	}
}
