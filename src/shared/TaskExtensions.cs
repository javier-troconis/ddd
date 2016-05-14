using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace shared
{
    public static class TaskExtensions
    {
        public static async Task<TOut> TimeoutAfter<TOut>(this Task<TOut> task, TimeSpan timeout)
        {
            //var tokenSource = new CancellationTokenSource(timeout);
            //return Task.Run(() =>
            //{
            //    do
            //    {
            //        tokenSource.Token.ThrowIfCancellationRequested();
            //    } while (!task.IsCompleted);
            //    return task.Result;
            //}, tokenSource.Token);

            var tokenSource = new CancellationTokenSource();
            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, tokenSource.Token));
            if (completedTask != task)
            {
                throw new TimeoutException("The operation has timed out.");
            }
            tokenSource.Cancel();
            return task.Result;
        }

   
    }
}
