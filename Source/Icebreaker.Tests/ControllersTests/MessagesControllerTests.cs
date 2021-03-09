// <copyright file="MessagesControllerTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.ControllersTests
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Icebreaker.Controllers;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Moq;
    using Xunit;

    public class MessagesControllerTests
    {
        private readonly MessagesController sut;
        private readonly Mock<IBotFrameworkHttpAdapter> botAdapter;
        private readonly Mock<IBot> bot;

        public MessagesControllerTests()
        {
            this.bot = new Mock<IBot>();
            this.botAdapter = new Mock<IBotFrameworkHttpAdapter>();
            this.botAdapter
                .Setup(x => x.ProcessAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<HttpResponseMessage>(), It.IsAny<IBot>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Create and initialize controller
            this.sut = new MessagesController(this.botAdapter.Object, this.bot.Object)
                { Request = new HttpRequestMessage(), Configuration = new HttpConfiguration() };
        }

        [Fact]
        public async Task PostAsync_MessageSent_BotHandlerInvoked()
        {
            // Act: Invoke the controller
            await this.sut.PostAsync();

            // Assert
            this.botAdapter.Verify(
                x => x.ProcessAsync(
                    It.IsAny<HttpRequestMessage>(),
                    It.IsAny<HttpResponseMessage>(),
                    It.Is<IBot>(o => o == this.bot.Object),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}