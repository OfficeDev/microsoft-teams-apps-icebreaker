// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Controllers
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// API controller to process matches.
    /// </summary>
    [Route("api/processnow")]
    [ApiController]
    public class ProcessNowController : ControllerBase
    {
        private const string KeyHeaderName = "X-Key";
        private readonly IMatchingService matchingService;
        private readonly ISecretsProvider secretsProvider;
        private readonly IBackgroundTaskQueue backgroundHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNowController"/> class.
        /// </summary>
        /// <param name="matchingService">Matching service contains logic to pair and match users</param>
        /// <param name="secretsProvider">To fetch secrets</param>
        public ProcessNowController(IMatchingService matchingService, ISecretsProvider secretsProvider, IBackgroundTaskQueue helper)
        {
            this.matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));
            this.backgroundHelper = helper;
        }

        /// <summary>
        /// Action to process matches
        /// </summary>
        /// <returns>Success (1) or failure (-1) code</returns>

        [HttpGet]
        public async Task<ActionResult> GetAsync()
        {
            if (this.Request.Headers.TryGetValue(KeyHeaderName, out var keys))
            {
                var isKeyMatch = keys.Any() && object.Equals(keys.First(), this.secretsProvider.GetLogicAppKey());
                if (isKeyMatch)
                {
                    await this.RefreshToken();

                    this.backgroundHelper.EnqueueTask(async token =>
                    {
                        await this.MakePairsAsync();
                    });

                    return new OkObjectResult("Queued the matching pairs task for background processing");
                }
            }

            return this.Unauthorized();
        }

        private async Task<int> MakePairsAsync()
        {
            return await this.matchingService.MakePairsAndNotifyAsync();
        }

        private async Task RefreshToken()
        {
            // Get the token here to proactively trigger a refresh if the cached token is expired
            // This avoids a race condition in MicrosoftAppCredentials.GetTokenAsync that can lead it to return an expired token
            var appCredentials = await this.secretsProvider.GetAppCredentialsAsync();
            await appCredentials.GetTokenAsync();
        }
    }
}