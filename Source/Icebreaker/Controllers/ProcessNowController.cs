//----------------------------------------------------------------------------------------------
// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Controllers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.Azure;
    using Microsoft.Bot.Connector;

    /// <summary>
    /// API controller to process matches.
    /// </summary>
    public class ProcessNowController : ApiController
    {
        private const string KeyHeaderName = "X-Key";
        private readonly IcebreakerBot bot;
        private readonly MicrosoftAppCredentials botCredentials;
        private readonly string apiKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNowController"/> class.
        /// </summary>
        /// <param name="bot">The Icebreaker bot instance</param>
        /// <param name="botCredentials">The bot AAD credentials</param>
        public ProcessNowController(IcebreakerBot bot, MicrosoftAppCredentials botCredentials)
        {
            this.bot = bot;
            this.botCredentials = botCredentials;
            this.apiKey = CloudConfigurationManager.GetSetting("Key");
        }

        /// <summary>
        /// Action to process matches
        /// </summary>
        /// <returns>Success (1) or failure (-1) code</returns>
        [Route("api/processnow")]
        public async Task<IHttpActionResult> Get()
        {
            IEnumerable<string> keys;
            if (this.Request.Headers.TryGetValues(KeyHeaderName, out keys))
            {
                var isKeyMatch = keys.Any() && object.Equals(keys.First(), this.apiKey);
                if (isKeyMatch)
                {
                    // Get the token here to proactively trigger a refresh if the cached token is expired
                    // This avoids a race condition in MicrosoftAppCredentials.GetTokenAsync that can lead it to return an expired token
                    await this.botCredentials.GetTokenAsync();

                    HostingEnvironment.QueueBackgroundWorkItem(ct => this.MakePairs());
                    return this.StatusCode(System.Net.HttpStatusCode.OK);
                }
            }

            return this.Unauthorized();
        }

        private async Task<int> MakePairs()
        {
            return await this.bot.MakePairsAndNotify();
        }
    }
}