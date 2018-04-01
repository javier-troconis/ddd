using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using shared;
using System.Collections.ObjectModel;

namespace eventstore
{
    public interface ISubscriberRegistration
    {

    }

    public interface ISubscriberRegistry : IReadOnlyDictionary<string, ISubscriberRegistration>
    {
        
    }
}
