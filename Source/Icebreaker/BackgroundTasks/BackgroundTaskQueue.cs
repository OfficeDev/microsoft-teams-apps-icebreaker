// <copyright file="BackgroundTaskQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.BackgroundTasks
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;

    /// <summary>
    /// BackgroundTaskQueue
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> tasks =
                            new ConcurrentQueue<Func<CancellationToken, Task>>();

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);

        /// <summary>
        /// EnqueueTask
        /// </summary>
        /// <param name="task">task to enqueue</param>
        public void EnqueueTask(Func<CancellationToken, Task> task)
        {
            try
            {
                if (task == null)
                {
                    throw new ArgumentNullException(nameof(task));
                }

                this.tasks.Enqueue(task);
                this.semaphore.Release();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// DequeueTaskAsync
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Task</returns>
        public async Task<Func<CancellationToken, Task>> DequeueTaskAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.semaphore.WaitAsync(cancellationToken);
                this.tasks.TryDequeue(out var task);
                return task;
            }
            catch
            {
                throw;
            }
        }
    }
}
