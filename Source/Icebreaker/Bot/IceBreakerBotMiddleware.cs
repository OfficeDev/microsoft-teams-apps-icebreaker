// <copyright file="IceBreakerBotMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Bot
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.Bot.Builder;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// IceBreakerBotMiddleware
    /// </summary>
    public class IceBreakerBotMiddleware : IMiddleware
    {
        private readonly IAppSettings appSettings;
        private readonly ILogger<IceBreakerBotMiddleware> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="IceBreakerBotMiddleware"/> class.
        /// </summary>
        /// <param name="appSettings">appSettings</param>
        public IceBreakerBotMiddleware(IAppSettings appSettings, ILogger<IceBreakerBotMiddleware> logger)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!this.IsTenantAllowed(turnContext))
                {
                    this.logger.LogInformation("The current tenant is not allowed to proceed.");
                    return;
                }

                // Get the current culture info to use in resource files
                string locale = turnContext?.Activity.Entities?.FirstOrDefault(entity => entity.Type == "clientInfo")?.Properties["locale"]?.ToString();

                if (!string.IsNullOrEmpty(locale))
                {
                    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                }

                await next(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Exception occured in the middleware.", ex.ToString());
                throw;
            }
        }

        private bool IsTenantAllowed(ITurnContext turnContext)
        {
            var tenantId = turnContext?.Activity?.Conversation?.TenantId;
            this.logger.LogInformation($"TeanantId {tenantId}");

            if (this.appSettings.DisableTenantFilter)
            {
                this.logger.LogInformation("Tenant filter is disabled.");
                return true;
            }

            var allowedTenantIds = this.appSettings.AllowedTenantIds;
            if (allowedTenantIds == null || !allowedTenantIds.Any())
            {
                this.logger.LogError("AllowedTenants setting is not set properly in the configuration file.");
                return false;
            }

            return allowedTenantIds.Contains(tenantId);
        }
    }
}
