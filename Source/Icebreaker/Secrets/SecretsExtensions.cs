// <copyright file="SecretsExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Secrets
{
    using System;
    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using Icebreaker.Interfaces;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    public static class SecretsExtensions
    {
        /// <summary>
        /// Service Collection extension.
        ///
        /// Injects secrets provider.
        /// </summary>
        /// <param name="services">Servie collection.</param>
        /// <param name="configuration">Configuration.</param>
        /// <returns>Service collection.</returns>
        public static IServiceCollection AddSecretsProvider(this IServiceCollection services, IConfiguration configuration)
        {
            var keyVaultUrl = configuration.GetValue<string>("KeyVaultUri");
            var options = new CertificateClientOptions();
            options.AddPolicy(new KeyVaultProxy(), HttpPipelinePosition.PerCall);
            services.AddSingleton(new CertificateClient(new Uri(keyVaultUrl), new DefaultAzureCredential(), options));
            services.AddSingleton<ISecretsProvider, SecretsProvider>();

            return services;
        }
    }
}
