using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Newtonsoft.Json.Linq;

namespace infra
{
    public interface ISubscription
    {
        Task StartAsync();

    }

    //internal abstract class Subscription
    //{
    //    public async Task StartAsync()
    //    {
    //        await EnsureSourceStreamAsync();
    //        await SubscribeAsync(0, 5000);
    //    }

    //    protected async Task SubscribeAsync(int delayInMilliseconds, int maxDelayInMilliseconds)
    //    {
    //        while (true)
    //        {
    //            await Task.Delay(maxDelayInMilliseconds > delayInMilliseconds ? delayInMilliseconds : maxDelayInMilliseconds);
    //            try
    //            {
    //                await DoSubscribeAsync();
    //            }
    //            catch (Exception ex)
    //            {
    //                delayInMilliseconds = delayInMilliseconds * 2;
    //                continue;
    //            }
    //            break;
    //        }
    //    }

    //    protected abstract Task DoSubscribeAsync();

    //    protected abstract Task EnsureSourceStreamAsync();
    //}

    internal class CatchUpSubscription<TSubscriber> : ISubscription
    {
        private readonly IEventStoreConnection _connection;
        private readonly Func<TSubscriber> _createSubscriber;
        private readonly Func<Task<int?>> _getLastCheckpoint;

        public CatchUpSubscription(IEventStoreConnection connection, Func<TSubscriber> createSubscriber, Func<Task<int?>> getLastCheckpoint)
        {
            _connection = connection;
            _createSubscriber = createSubscriber;
            _getLastCheckpoint = getLastCheckpoint;
        }

        public async Task StartAsync()
        {
            await StartAsync(0, 5000);
        }

        private async Task StartAsync(int delayInMilliseconds, int maxDelayInMilliseconds)
        {
            var lastCheckpoint = await _getLastCheckpoint();
            _connection.SubscribeToStreamFrom(NamingConvention.Subscription<TSubscriber>(), lastCheckpoint, CatchUpSubscriptionSettings.Default, 
                eventAppeared: OnEventAppeared, subscriptionDropped: OnSubscriptionDropped);
        }

        private async void OnSubscriptionDropped(EventStoreCatchUpSubscription subscription, SubscriptionDropReason reason, Exception error)
        {
            await StartAsync();
        }

        private void OnEventAppeared(EventStoreCatchUpSubscription subscription, ResolvedEvent resolvedEvent)
        {
            //var recordedEvent = resolvedEvent.Event;
            //var eventMetadata = JObject.Parse(Encoding.UTF8.GetString(recordedEvent.Metadata));
            //var eventClrTypeName = (string)eventMetadata.Property(EventStore.EventClrTypeHeader).Value;
            //var subscriber = _createSubscriber();

        }
    }

    internal class PersistentSubscription<TSubscriber> : ISubscription
    {
        private readonly IEventStoreConnection _connection;
        private readonly Func<TSubscriber> _createSubscriber;

        public PersistentSubscription(IEventStoreConnection connection, Func<TSubscriber> createSubscriber)
        {
            _connection = connection;
            _createSubscriber = createSubscriber;
        }

        public Task StartAsync()
        {
            throw new NotImplementedException();
        }
    }
}
