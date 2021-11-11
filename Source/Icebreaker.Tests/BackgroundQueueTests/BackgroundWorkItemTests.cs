// <copyright file="BackgroundWorkItemTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.BackgroundQueueTests
{
    using System;
    using Icebreaker.BackgroundTasks;
    using Icebreaker.Interfaces;
    using Moq;
    using Xunit;

    public class BackgroundWorkItemTests
    {
        [Fact]
        public async void WorkItemWith_EmptyTask_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => new BackgroundWorkItem(null, null));
        }

        [Fact]
        public async void WorkItemWith_TaskAndEmptyCorrelationId_CreatesGuid()
        {
            var matchingService = new Mock<IMatchingService>();
            var workItem = new BackgroundWorkItem(
                async token =>
                {
                    await matchingService.Object.MakePairsAndNotifyAsync().ConfigureAwait(false);
                }, null);
            Assert.NotNull(workItem.CorrelationId);
            Assert.True(Guid.TryParse(workItem.CorrelationId, out Guid guidOutput));
        }

        [Fact]
        public async void WorkItemWith_ValidParameters()
        {
            var matchingService = new Mock<IMatchingService>();
            var correlationId = Guid.NewGuid().ToString();
            var workItem = new BackgroundWorkItem(
                async token =>
                {
                    await matchingService.Object.MakePairsAndNotifyAsync().ConfigureAwait(false);
                }, correlationId);
            Assert.Equal(workItem.CorrelationId, correlationId);
        }
    }
}
