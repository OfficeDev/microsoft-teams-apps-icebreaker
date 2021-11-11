// <copyright file="IBackgroundTaskQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
    using Icebreaker.BackgroundTasks;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// IBackgroundTaskQueue
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        /// <summary>
        /// EnqueueTask
        /// </summary>
        /// <param name="item">task to enqueue</param>
        void EnqueueTask(BackgroundWorkItem item);

        /// <summary>
        /// DequeueTaskAsync
        /// </summary>
        /// <param name="cancellationToken">cancellation</param>
        /// <returns>Task</returns>
        Task<BackgroundWorkItem> DequeueTaskAsync(
            CancellationToken cancellationToken);
    }
}
