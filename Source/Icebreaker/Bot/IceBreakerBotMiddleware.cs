// <copyright file="IceBreakerBotMiddleware.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
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

    /// <summary>
    /// IceBreakerBotMiddleware
    /// </summary>
    public class IceBreakerBotMiddleware : IMiddleware
    {
        private readonly IAppSettings appSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="IceBreakerBotMiddleware"/> class.
        /// </summary>
        /// <param name="appSettings">appSettings</param>
        public IceBreakerBotMiddleware(IAppSettings appSettings)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }

        /// <inheritdoc/>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            try
            {
                // ToDo - add Ilogger
                if (!this.IsTenantAllowed(turnContext))
                {
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
                // this.telemetryClient.TrackException(ex);
            }
        }

        private bool IsTenantAllowed(ITurnContext turnContext)
        {
            if (this.appSettings.DisableTenantFilter)
            {
                return true;
            }

            var allowedTenantIds = this.appSettings.AllowedTenantIds;
            if (allowedTenantIds == null || !allowedTenantIds.Any())
            {
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.";
                throw new ApplicationException(exceptionMessage);
            }

            var tenantId = turnContext?.Activity?.Conversation?.TenantId;
            return allowedTenantIds.Contains(tenantId);
        }
    }
}
