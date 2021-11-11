// <copyright file="ProcessNowControllerTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.ControllersTests
{
    using System;
    using System.Threading.Tasks;
    using Icebreaker.Controllers;
    using Icebreaker.Interfaces;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;

    /// <summary>
    /// Unit tests for the <see cref="ProcessNowController"/> class.
    /// </summary>
    public class ProcessNowControllerTests
    {
        private readonly ProcessNowController sut;
        private readonly string apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNowControllerTests"/> class.
        /// </summary>
        public ProcessNowControllerTests()
        {
            this.apiKey = Guid.NewGuid().ToString();
            var matchingService = new Mock<IMatchingService>();
            var secretsProvider = new Mock<ISecretsProvider>();
            var backgroundQueue = new Mock<IBackgroundTaskQueue>();
            secretsProvider.Setup(x => x.GetLogicAppKey()).Returns(this.apiKey);
            var logger = new Mock<ILogger<ProcessNowController>>();

            // Create and initialize controller
            this.sut = new ProcessNowController(matchingService.Object, secretsProvider.Object, backgroundQueue.Object, logger.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext(),
                },
            };
        }

        [Fact]
        public async Task GetAsync_NoKeyPassed_ReturnsUnAuthorized()
        {
            // Act: Invoke the controller
            var response = await this.sut.GetAsync();

            // Assert
            Assert.IsType<UnauthorizedResult>(response);
        }

        [Fact]
        public async Task GetAsync_InvalidKeyPassed_ReturnsUnAuthorized()
        {
            this.sut.Request.Headers.Add("X-Key",  Guid.Empty.ToString());

            // Act: Invoke the controller
            var response = await this.sut.GetAsync();

            // Assert
            Assert.IsType<UnauthorizedResult>(response);
        }

        [Fact]
        public async Task GetAsync_ValidKeyPassed_AppCredentialsThrowsException()
        {
            this.sut.Request.Headers.Add("X-Key",  this.apiKey );

            // Act: Invoke the controller
            await Assert.ThrowsAsync<NullReferenceException>(async () => await this.sut.GetAsync());
        }
    }
}
