//----------------------------------------------------------------------------------------------
// <copyright file="ProcessNowController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace MeetupBot.Controllers
{
    using System.Threading.Tasks;
    using System.Web.Hosting;
    using System.Web.Http;
    using Microsoft.Azure;

    public class ProcessNowController : ApiController
    {
        // GET api/<controller>/5
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