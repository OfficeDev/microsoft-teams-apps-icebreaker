// <copyright file="BackgroundTaskItem.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.BackgroundTasks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// BackgroundTaskItem comprises of background task properties
    /// </summary>
    public sealed class BackgroundWorkItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundWorkItem"/> class.
        /// </summary>
        /// <param name="task">background task</param>
        /// <param name="correlationId">unique correlation id to track a request</param>
        public BackgroundWorkItem(Func<CancellationToken, Task> task, string correlationId)
        {
            this.WorkItem = task ?? throw new ArgumentNullException(nameof(task));
            this.CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets the workitem as a function.
        /// </summary>
        public Func<CancellationToken, Task> WorkItem { get; }

        /// <summary>
        /// Gets the unique CorrelationId for each work item.
        /// </summary>
        public string CorrelationId { get; }
    }
}
