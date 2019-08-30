//----------------------------------------------------------------------------------------------
// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;

    /// <summary>
    /// Controller for the bot messaging endpoint
    /// </summary>
    [Route("api/messages")]
    public class MessagesController : ApiController
    {
        private readonly IBotFrameworkHttpAdapter adapter;
        private readonly IBot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagesController"/> class.
        /// </summary>
        /// <param name="adapter">The bot framework adapter.</param>
        /// <param name="bot">The interface for the bot.</param>
        public MessagesController(
            IBotFrameworkHttpAdapter adapter,
            IBot bot)
        {
            this.adapter = adapter;
            this.bot = bot;
        }

        /// <summary>
        /// Executing the Post Async method.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        [HttpPost]
        public async Task<HttpResponseMessage> PostAsync()
        {
            var response = new HttpResponseMessage();
            await this.adapter.ProcessAsync(this.Request, response, this.bot);
            return response;
        }
    }
}