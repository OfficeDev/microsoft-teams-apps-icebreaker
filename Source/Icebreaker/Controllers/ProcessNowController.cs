// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Icebreaker.Interfaces;

    /// <summary>
    /// API controller to process matches.
    /// </summary>
    public class ProcessNowController : ApiController
    {
        private const string KeyHeaderName = "X-Key";
        private readonly IMatchingService matchingService;
        private readonly ISecretsProvider secretsProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNowController"/> class.
        /// </summary>
        /// <param name="matchingService">Matching service contains logic to pair and match users</param>
        /// <param name="secretsProvider">To fetch secrets</param>
        public ProcessNowController(IMatchingService matchingService, ISecretsProvider secretsProvider)
        {
            this.matchingService = matchingService ?? throw new ArgumentNullException(nameof(matchingService));
            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));
        }

        /// <summary>
        /// Action to process matches
        /// </summary>
        /// <returns>Success (1) or failure (-1) code</returns>
        [Route("api/processnow")]
        public async Task<IHttpActionResult> GetAsync()
        {
            if (this.Request.Headers.TryGetValues(KeyHeaderName, out IEnumerable<string> keys))
            {
                var isKeyMatch = keys.Any() && object.Equals(keys.First(), this.secretsProvider.GetLogicAppKey());
                if (isKeyMatch)
                {
                    await this.RefreshToken();
                    HostingEnvironment.QueueBackgroundWorkItem(ct => this.MakePairsAsync());
                    return this.StatusCode(System.Net.HttpStatusCode.OK);
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