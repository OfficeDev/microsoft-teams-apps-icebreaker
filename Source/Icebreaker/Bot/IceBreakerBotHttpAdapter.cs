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
    public class IceBreakerBotHttpAdapter : BotFrameworkHttpAdapter
    {
        private readonly ISecretsProvider secretsProvider;
        private readonly ILogger<IceBreakerBotHttpAdapter> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IceBreakerBotHttpAdapter"/> class.
        /// </summary>
        /// <param name="credentialProvider">credentialProvider</param>
        /// <param name="secretsProvider">secretsProvider</param>
        /// <param name="botMiddleware">botMiddleWare</param>
        public IceBreakerBotHttpAdapter(
            ICredentialProvider credentialProvider,
            ISecretsProvider secretsProvider,
            IceBreakerBotMiddleware botMiddleware)
           : base(credentialProvider)
        {
            if (botMiddleware is null)
            {
                throw new ArgumentNullException(nameof(botMiddleware));
            }

            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));

            // Middleware
            this.Use(botMiddleware);
        }

        /// <inheritdoc/>
        protected override async Task<AppCredentials> BuildCredentialsAsync(string appId, string oAuthScope = null)
        {
            this.logger.LogInformation("GetAppCredentials from IceBreakerBotHttpAdapter");
            var appCredentials = await this.secretsProvider.GetAppCredentialsAsync();
            var token = await appCredentials.GetTokenAsync();
            this.logger.LogInformation(String.IsNullOrEmpty(token).ToString());

            return appCredentials;
        }
    }
}
