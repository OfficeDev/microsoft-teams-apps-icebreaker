// <copyright file="IBackgroundTaskQueue.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
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
        /// <param name="task">task to enqueue</param>
        void EnqueueTask(Func<CancellationToken, Task> task);

        /// <summary>
        /// DequeueTaskAsync
        /// </summary>
        /// <param name="cancellationToken">cancellation</param>
        /// <returns>Task</returns>
        Task<Func<CancellationToken, Task>> DequeueTaskAsync(
            CancellationToken cancellationToken);
    }
}
