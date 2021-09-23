// <copyright file="LocalizationExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Localization
{
    using System.Collections.Generic;
    using System.Globalization;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Localization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Locaalization extensions
    /// </summary>
    public static class LocalizationExtensions
    {
        /// <summary>
        /// Service collection extension
        /// </summary>
        /// <param name="services">service collection</param>
        /// <param name="configuration">configuration</param>
        /// <returns>service collection</returns>
        public static IServiceCollection AddLocalization(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddLocalization();

            // Configure localization options.
            var defaultCulture = configuration.GetValue<string>("DefaultCulture", "en-US");
            var supportedCulture = configuration.GetValue<string>("SupportedCultures", "en-US");

            services.Configure<RequestLocalizationOptions>(
                options =>
                {
                    var supportedCultures = new List<CultureInfo>
                    {
                        new CultureInfo(supportedCulture),
                    };

                    options.DefaultRequestCulture = new RequestCulture(culture: defaultCulture, uiCulture: defaultCulture);
                    options.SupportedCultures = supportedCultures;
                    options.SupportedUICultures = supportedCultures;
                });

            return services;
        }
    }
}
