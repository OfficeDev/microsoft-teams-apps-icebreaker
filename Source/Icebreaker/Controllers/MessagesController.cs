// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;

    /// <summary>
    /// Controller for the bot messaging endpoint
    /// </summary>
    public class MessagesController : ControllerBase
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
            this.adapter = adapter ?? throw new System.ArgumentNullException(nameof(adapter));
            this.bot = bot ?? throw new System.ArgumentNullException(nameof(bot));
        }

        /// <summary>
        /// Action to process bot messages
        /// </summary>
        /// <returns>Bot compliant message card</returns>
        [HttpPost("api/messages")]
        public async Task PostAsync()
        {
            // Delegate the processing of the HTTP POST to the adapter.
            // The adapter will invoke the bot.
            await this.adapter.ProcessAsync(httpRequest: this.Request, httpResponse: this.Response, this.bot);
        }
    }
}