// <copyright file="ProcessNowControllerTests.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Tests.ControllersTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Results;
    using Icebreaker.Controllers;
    using Icebreaker.Interfaces;
    using Microsoft.Bot.Connector.Authentication;
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
            var secretsHelper = new Mock<ISecretsHelper>();
            secretsHelper.Setup(x => x.Key).Returns(this.apiKey);
            var appCredentials =
                new Mock<MicrosoftAppCredentials>(MockBehavior.Default, string.Empty, string.Empty);

            // Create and initialize controller
            this.sut = new ProcessNowController(matchingService.Object, appCredentials.Object, secretsHelper.Object)
            {
                Request = new HttpRequestMessage(),
                Configuration = new HttpConfiguration(),
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
            this.sut.Request.Headers.Add("X-Key", new List<string> { Guid.Empty.ToString() });

            // Act: Invoke the controller
            var response = await this.sut.GetAsync();

            // Assert
            Assert.IsType<UnauthorizedResult>(response);
        }

        [Fact]
        public async Task GetAsync_ValidKeyPassed_AppCredentialsThrowsException()
        {
            this.sut.Request.Headers.Add("X-Key", new List<string> { this.apiKey });

            // Act: Invoke the controller
            await Assert.ThrowsAsync<NullReferenceException>(async () => await this.sut.GetAsync());
        }
    }
}
