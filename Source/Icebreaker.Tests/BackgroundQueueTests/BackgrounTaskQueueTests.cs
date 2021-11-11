// <copyright file="BackgrounTaskQueueTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.BackgroundQueueTests
{
    using System;
    using System.Threading;
    using Icebreaker.BackgroundTasks;
    using Icebreaker.Interfaces;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    public class BackgrounTaskQueueTests
    {
        private readonly BackgroundTaskQueue backgroundTaskQueue;
        private readonly BackgroundWorkItem backgroundWorkItem;

        public BackgrounTaskQueueTests()
        {
            var logger = new Mock<ILogger<BackgroundTaskQueue>>();
            this.backgroundTaskQueue = new BackgroundTaskQueue(logger.Object);
            var matchingService = new Mock<IMatchingService>();

            this.backgroundWorkItem = new BackgroundWorkItem(
                async token =>
                {
                    await matchingService.Object.MakePairsAndNotifyAsync().ConfigureAwait(false);
                }, null);
        }

        [Fact]
        public async void EnqueueNullWorkItem_ThrowsArgNullException()
        {
            Assert.Throws<ArgumentNullException>(() => this.backgroundTaskQueue.EnqueueTask(null));
        }

        [Fact]
        public async void TestEnqueueAndDequeueWorkItems()
        {
            this.backgroundTaskQueue.EnqueueTask(this.backgroundWorkItem);
            var dequeuedWorkItem = await this.backgroundTaskQueue.DequeueTaskAsync(CancellationToken.None);
            Assert.Equal(this.backgroundWorkItem, dequeuedWorkItem);
        }
    }
}
