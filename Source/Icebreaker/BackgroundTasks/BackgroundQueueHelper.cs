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
    public class BackgroundQueueHelper : BackgroundService
    {

        private readonly ILogger<BackgroundQueueHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundQueueHelper"/> class.
        /// </summary>
        /// <param name="taskQueue">tasks</param>
        /// <param name="logger">logger</param>
        public BackgroundQueueHelper(IBackgroundTaskQueue taskQueue, ILogger<BackgroundQueueHelper> logger)
        {
            this.TaskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            this.logger = logger;
        }

        /// <summary>
        /// Gets taskQueue
        /// </summary>
        public IBackgroundTaskQueue TaskQueue { get; }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.logger.LogInformation("Dequeuing the task");
                    var currentTask = await this.TaskQueue.DequeueTaskAsync(cancellationToken);
                    await currentTask(cancellationToken);
                    this.logger.LogInformation("Completed Executing the task");
                }
                catch (Exception ex)
                {
                    this.logger.LogError($"Exception {ex.Message} occured while dequeuing the task.", ex);
                }
            }
        }
    }
}
