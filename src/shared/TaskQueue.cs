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

    public sealed class TaskQueue
    {
        private readonly ConcurrentDictionary<string, Lazy<Channel>> _channels = new ConcurrentDictionary<string, Lazy<Channel>>();

        public Task<bool> SendToChannel(string channelName, Func<Task> getTask, TaskSucceeded taskSucceeded = null, TaskFailed taskFailed = null)
        {
            var channel = _channels.GetOrAdd(channelName, new Lazy<Channel>(() => new Channel(channelName))).Value;
            return channel.SendAsync(
                getTask,
                c => taskSucceeded?.Invoke(c),
                (c, ex) => taskFailed?.Invoke(c, ex));
        }

        public void CancelChannel(string channelName)
        {
            _channels[channelName].Value.Cancel();
        }

        public Task CompleteChannelAsync(string channelName)
        {
            return _channels[channelName].Value.CompleteAsync();
        }

        private class Channel
        {
            private readonly CancellationTokenSource _queueCancellationTokenSource = new CancellationTokenSource();
            private readonly ActionBlock<Func<string, Task>> _queue;

            public Channel(string name)
            {
                _queue = new ActionBlock<Func<string, Task>>(
                    x => x(name),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = 1,
                        CancellationToken = _queueCancellationTokenSource.Token
                    });
            }

            public Task<bool> SendAsync(Func<Task> getTask, TaskSucceeded taskSucceeded, TaskFailed taskFailed)
            {
                return _queue.SendAsync(async name =>
                {
                    try
                    {
                        await getTask();
                    }
                    catch (Exception ex)
                    {
                        taskFailed(name, ex);
                        return;
                    }
                    taskSucceeded(name);
                });
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
