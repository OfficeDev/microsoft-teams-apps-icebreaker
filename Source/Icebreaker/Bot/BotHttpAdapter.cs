// <copyright file="BotHttpAdapter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Bot
{
    using System;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// BotHttpAdapter
    /// </summary>
    public class BotHttpAdapter : BotFrameworkHttpAdapter
    {
        private readonly ISecretsProvider secretsProvider;
        private readonly ILogger<BotHttpAdapter> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BotHttpAdapter"/> class.
        /// </summary>
        /// <param name="appSettings">appSettings</param>
        /// <param name="credentialProvider">credentialProvider</param>
        /// <param name="secretsProvider">secretsProvider</param>
        /// <param name="botMiddleWare">botMiddleWare</param>
        public BotHttpAdapter(
            IAppSettings appSettings,
            ICredentialProvider credentialProvider,
            ISecretsProvider secretsProvider,
            IceBreakerBotMiddleware botMiddleWare)
           : base(credentialProvider)
        {
            if (botMiddleWare is null)
            {
                throw new ArgumentNullException(nameof(botMiddleWare));
            }

            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));

            // Middleware
            this.Use(botMiddleWare);
        }

        /// <inheritdoc/>
        protected override async Task<AppCredentials> BuildCredentialsAsync(string appId, string oAuthScope = null)
        {
            var appCredentials = await this.secretsProvider.GetAppCredentialsAsync();
            return appCredentials;
        }
    }
}
