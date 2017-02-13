using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace shared
{
    public delegate void TaskSucceeded(string channelName);

    public delegate void TaskFailed(string channelName, Exception ex);

    public static class TaskQueue
    {
        private static readonly ConcurrentDictionary<string, Lazy<Channel>> _channels = new ConcurrentDictionary<string, Lazy<Channel>>();

        public static Task<bool> SendToChannelAsync(string channelName, Func<Task> getTask, TaskSucceeded taskSucceeded = null, TaskFailed taskFailed = null)
        {
            var channel = _channels.GetOrAdd(channelName, new Lazy<Channel>(() => new Channel(channelName)));
            return channel.Value.SendAsync(getTask, taskSucceeded, taskFailed);
        }

        public static void CancelChannel(string channelName)
        {
            _channels[channelName].Value.Cancel();
        }

        public static Task CompleteChannelAsync(string channelName)
        {
            return _channels[channelName].Value.CompleteAsync();
        }

        private class Channel
        {
            private readonly CancellationTokenSource _queueCancellationTokenSource = new CancellationTokenSource();
            private readonly ActionBlock<Tuple<Func<Task>, TaskSucceeded, TaskFailed>> _queue;

            public Channel(string name)
            {
                _queue = new ActionBlock<Tuple<Func<Task>, TaskSucceeded, TaskFailed>>(async x =>
                {
                    try
                    {
                        await x.Item1();
                    }
                    catch (Exception ex)
                    {
                        x.Item3?.Invoke(name, ex);
                        return;
                    }
                    x.Item2?.Invoke(name);
                },
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = 1,
                        CancellationToken = _queueCancellationTokenSource.Token
                    });
            }

            public Task<bool> SendAsync(Func<Task> getTask, TaskSucceeded taskSucceeded, TaskFailed taskFailed)
            {
                return _queue.SendAsync(Tuple.Create(getTask, taskSucceeded, taskFailed));
            }

            public void Cancel()
            {
                _queueCancellationTokenSource.Cancel();
            }

            public Task CompleteAsync()
            {
                _queue.Complete();
                return _queue.Completion;
            }
        }
    }
}
