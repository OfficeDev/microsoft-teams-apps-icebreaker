//----------------------------------------------------------------------------------------------
// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.Azure;

    /// <summary>
    /// API controller to process matches.
    /// </summary>
    public class ProcessNowController : ApiController
    {
        /// <summary>
        /// Action to process matches
        /// </summary>
        /// <param name="key">API key</param>
        /// <returns>Success (1) or failure (-1) code</returns>
        [Route("api/processnow/{key}")]
        public int Get([FromUri]string key)
        {
            if (object.Equals(key, CloudConfigurationManager.GetSetting("Key")))
            {
                HostingEnvironment.QueueBackgroundWorkItem(ct => MakePairs());
                return 1;
            }
            else
            {
                return -1;
            }
        }

        private static async Task<int> MakePairs()
        {
            return await IcebreakerBot.MakePairsAndNotify();
        }
    }
}