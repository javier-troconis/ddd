using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace shared
{
    public static class TaskExtensions
    {
        public static async Task<TOut> WithCancellation<TOut>(this Task<TOut> task, CancellationToken cancellationToken)
        {
            var cancellableTaskSource = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancellableTaskSource.TrySetResult(true)))
            {
                var completedTask = await Task.WhenAny(task, cancellableTaskSource.Task);
                if (completedTask != task)
                {
                    throw new TaskCanceledException(task);
                }
            }
            return await task;
        }
    }
}
