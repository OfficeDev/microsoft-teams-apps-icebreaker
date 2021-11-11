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
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// BackgroundTaskQueue contains the core logic of enqueuing and dequeuing a work item.
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue
    {
        private readonly ConcurrentQueue<BackgroundWorkItem> backgroundWorkItems = new ConcurrentQueue<BackgroundWorkItem>();

        private readonly SemaphoreSlim semaphore = new SemaphoreSlim(0);

        private readonly ILogger<BackgroundTaskQueue> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundTaskQueue"/> class.
        /// </summary>
        /// <param name="logger">Logger to use</param>
        public BackgroundTaskQueue(ILogger<BackgroundTaskQueue> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Enqueue's an incoming workitem and releases the semaphore slim.
        /// </summary>
        /// <param name="workItem">workitem to enqueue</param>
        public void EnqueueTask(BackgroundWorkItem workItem)
        {
            try
            {
                if (workItem == null)
                {
                    this.logger.LogInformation("Received null workItem to enqueue");
                    throw new ArgumentNullException(nameof(workItem));
                }

                this.logger.LogInformation("Enqueuing workitem");
                this.backgroundWorkItems.Enqueue(workItem);
                this.semaphore.Release();
            }
            catch (Exception ex)
            {
                this.logger.LogError("Exception occured while enqueuing the background task", ex);
                throw;
            }
        }

        /// <summary>
        /// DequeueTaskAsync dequeues a workitem from the concurrent queue on a semaphore slim lock.
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>Task</returns>
        public async Task<BackgroundWorkItem> DequeueTaskAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this.semaphore.WaitAsync(cancellationToken);

                if (!this.backgroundWorkItems.TryDequeue(out var task))
                {
                    this.logger.LogInformation("The task is already dequeued.");
                    this.semaphore.Release();
                    return null;
                }

                return task;
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Exception occured while dequeuing the background task", ex);
                throw;
            }
        }
    }
}
