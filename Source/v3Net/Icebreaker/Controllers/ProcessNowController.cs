//----------------------------------------------------------------------------------------------
// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure;

    /// <summary>
    /// API controller to process matches.
    /// </summary>
    public class ProcessNowController : ApiController
    {
        private readonly IcebreakerBot bot;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessNowController"/> class.
        /// </summary>
        /// <param name="bot">The Icebreaker bot instance</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public ProcessNowController(IcebreakerBot bot, TelemetryClient telemetryClient)
        {
            this.bot = bot;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Action to process matches
        /// </summary>
        /// <param name="key">API key</param>
        /// <returns>Success (1) or failure (-1) code</returns>
        [Route("api/processnow/{key}")]
        public int Get([FromUri]string key)
        {
            var keyMatches = object.Equals(key, CloudConfigurationManager.GetSetting("Key"));

            var parameters = new Dictionary<string, string>
            {
                { "KeyMatches", keyMatches.ToString() },
            };
            this.telemetryClient.TrackEvent("ProcessNowRequest", parameters);

            if (keyMatches)
            {
                HostingEnvironment.QueueBackgroundWorkItem(ct => this.MakePairs());
                return 1;
            }
            else
            {
                return -1;
            }
        }

        private async Task<int> MakePairs()
        {
            return await this.bot.MakePairsAndNotify();
        }
    }
}