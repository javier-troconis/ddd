﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using shared;
using ImpromptuInterface;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;

namespace eventstore
{
    public enum StopSubscriberResult
    {
        NotFound,
        Stopped
    }

    public enum StartSubscriberResult
    {
        NotFound,
        Started
    }

    public interface IEventBus
    {
        Task<StopSubscriberResult> StopSubscriber(string subscriberName);
        Task StopAllSubscribers();
        Task<StartSubscriberResult> StartSubscriber(string subscriberName);
        Task StartAllSubscribers();
    }

    public sealed class EventBus : IEventBus
    {
        private readonly TaskQueue _queue = new TaskQueue();
        private readonly IDictionary<string, Delegate> _subscribers;

        public EventBus(Func<IEventStoreConnection> createConnection, ISubscriberRegistry subscriberRegistry)
        {
            _subscribers = 
                subscriberRegistry
                    .ToDictionary
                    (
                        x => x.Key,
                        x =>
                        {
                            async Task<DisconnectSubscriber> ConnectSubscriber(Action<SubscriptionDropReason> subscriptionDropped)
                            {
                                var connection = await x.Value
                                (
	                                createConnection, (dropReason, exception) => subscriptionDropped(dropReason)
                                );
                                return () =>
                                {
                                    connection.Disconnect();
                                    return ConnectSubscriber;
                                };
                            }
                            return (Delegate)new ConnectSubscriber(ConnectSubscriber);
                        }
                    );
        }

        public async Task<StopSubscriberResult> StopSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return StopSubscriberResult.NotFound;
            }

            var tsc = new TaskCompletionSource<StopSubscriberResult>();
            await _queue.SendToChannel
            (
                subscriberName,
                () =>
                {
                    DisconnectSubscriber disconnectSubscriber;
                    if ((disconnectSubscriber = _subscribers[subscriberName] as DisconnectSubscriber) != null)
                    {
                        _subscribers[subscriberName] = disconnectSubscriber();
                    }
                    return Task.CompletedTask;
                },
                taskSucceeded: x => tsc.SetResult(StopSubscriberResult.Stopped)
            );
            return await tsc.Task;
        }

        public Task StopAllSubscribers()
        {
            return Task.WhenAll(_subscribers.Select(x => StopSubscriber(x.Key)));
        }

        public async Task<StartSubscriberResult> StartSubscriber(string subscriberName)
        {
            if (!_subscribers.ContainsKey(subscriberName))
            {
                return StartSubscriberResult.NotFound;
            }

            var tsc = new TaskCompletionSource<StartSubscriberResult>();
            await _queue.SendToChannel
            (
                subscriberName,
                async () =>
                {
                    ConnectSubscriber connectSubscriber;
                    if ((connectSubscriber = _subscribers[subscriberName] as ConnectSubscriber) != null)
                    {
                        _subscribers[subscriberName] = await connectSubscriber
                        (
                            async dropReason =>
                            {
                                if (dropReason == SubscriptionDropReason.UserInitiated)
                                {
                                    return;
                                }
                                await StopSubscriber(subscriberName);
                                await StartSubscriber(subscriberName);
                            }
                        );
                    }
                },
                taskSucceeded: x => tsc.SetResult(StartSubscriberResult.Started)
            );
            return await tsc.Task;
        }

        public Task StartAllSubscribers()
        {
            return Task.WhenAll(_subscribers.Select(x => StartSubscriber(x.Key)));
        }

        private delegate Task<DisconnectSubscriber> ConnectSubscriber(Action<SubscriptionDropReason> subscriptionDropped);

        private delegate ConnectSubscriber DisconnectSubscriber();
    }
}
