using System.Threading.Tasks;

namespace eventstore
{
    public interface ISubscription
    {
	    Task Start();
    }
}
