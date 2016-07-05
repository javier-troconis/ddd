using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace infra
{
    public static class EventBus
    {
        public static ISubscription RegisterCatchUpSubscription<TSubscriber>(IEventStoreConnection connection, Func<TSubscriber> createSubscriber, Func<Task<int?>> getLastCheckpoint)
        {
            throw new NotImplementedException();
        }

        public static ISubscription RegisterPersistentSubscription<TSubscriber>(IEventStoreConnection connection, Func<TSubscriber> createSubscriber)
        {
            throw new NotImplementedException();
        }
    }
}
