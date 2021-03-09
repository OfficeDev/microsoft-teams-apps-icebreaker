// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Controllers
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;

    /// <summary>
    /// Controller for the bot messaging endpoint
    /// </summary>
    public class MessagesController : ApiController
    {
        private readonly IBotFrameworkHttpAdapter adapter;
        private readonly IBot bot;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagesController"/> class.
        /// </summary>
        /// <param name="adapter">Bot adapter.</param>
        /// <param name="bot">The Icebreaker bot instance</param>
        public MessagesController(IBotFrameworkHttpAdapter adapter, IBot bot)
        {
            this.adapter = adapter;
            this.bot = bot;
        }

        /// <summary>
        /// Action to process bot messages
        /// </summary>
        /// <returns>Bot compliant message card</returns>
        public async Task<HttpResponseMessage> PostAsync()
        {
            var response = new HttpResponseMessage();

            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await this.adapter.ProcessAsync(this.Request, response, this.bot);
            return response;
        }
    }
}