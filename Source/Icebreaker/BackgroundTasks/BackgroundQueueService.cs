// <copyright file="BackgroundQueueService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.BackgroundTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// BackgroundQueueHelper
    /// </summary>
    public class BackgroundQueueService : BackgroundService
    {
        private readonly ILogger<BackgroundQueueService> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundQueueService"/> class.
        /// </summary>
        /// <param name="taskQueue">tasks</param>
        /// <param name="logger">logger</param>
        public BackgroundQueueService(IBackgroundTaskQueue taskQueue, ILogger<BackgroundQueueService> logger)
        {
            this.TaskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            this.logger = logger;
        }

        /// <summary>
        /// Gets a TaskQueue interface object. This's used to enqueue and dequeue work items.
        /// </summary>
        public IBackgroundTaskQueue TaskQueue { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.logger.LogInformation("Executing background service task");
            while (!cancellationToken.IsCancellationRequested)
            {
                var backgroundItem = await this.TaskQueue.DequeueTaskAsync(cancellationToken);
                if (backgroundItem != null)
                {
                    using (this.logger.BeginScope($"Correlation Id: {backgroundItem.CorrelationId}"))
                    {
                        try
                        {
                            this.logger.LogInformation($"Executing the task.");
                            await backgroundItem.WorkItem(cancellationToken);
                            this.logger.LogInformation("Completed executing the task");
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError($"Exception {ex.Message} occured while executing the task.", ex);
                        }
                    }
                }
            }
        }
    }
}
