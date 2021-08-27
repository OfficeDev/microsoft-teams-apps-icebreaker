// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.WebApi;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Secrets;
    using Icebreaker.Bot;
    using Icebreaker.Helpers;
    using Icebreaker.Interfaces;
    using Icebreaker.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Microsoft.Bot.Connector.Authentication;
    using Module = Autofac.Module;

    /// <summary>
    /// Autofac Module
    /// </summary>
    public class IcebreakerModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterWebApiFilterProvider(GlobalConfiguration.Configuration);
            var appInsightsInstrumentationKey = CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY");
            var keyVaultUri = CloudConfigurationManager.GetSetting("KeyVaultUri");

            var disableTenantFilter = Convert.ToBoolean(CloudConfigurationManager.GetSetting("DisableTenantFilter"), CultureInfo.InvariantCulture);
            var allowedTenantIds = CloudConfigurationManager.GetSetting("AllowedTenants")?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim())
                .ToHashSet();

            // ToDo - Check for 401 error without simple credential provider
            builder.Register(c => new SimpleCredentialProvider(
                    CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                    CloudConfigurationManager.GetSetting("MicrosoftAppPassword")))
                .As<ICredentialProvider>()
                .SingleInstance();

            builder
                .Register(c => new AppSettings
                {
                    IsTesting = Convert.ToBoolean(CloudConfigurationManager.GetSetting("Testing")),
                    DisableTenantFilter = disableTenantFilter,
                    AllowedTenantIds = allowedTenantIds,
                    BotDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName"),
                    BotCertName = CloudConfigurationManager.GetSetting("BotCertificateName"),
                    MicrosoftAppId = CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                    AppBaseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain"),
                    CosmosDBEndpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl"), // TODO(Bhavya): Create a separate setting for DB
                    CosmosDBDatabaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName"),
                    CosmosCollectionTeams = CloudConfigurationManager.GetSetting("CosmosCollectionTeams"),
                    CosmosCollectionUsers = CloudConfigurationManager.GetSetting("CosmosCollectionUsers"),
                    AppInsightsInstrumentationKey = appInsightsInstrumentationKey,
                    DefaultCulture = CloudConfigurationManager.GetSetting("DefaultCulture"),
                    MaxPairUpsPerTeam = int.Parse(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam")),
                })
                .As<IAppSettings>()
                .SingleInstance();

            builder.Register(c => new SecretOptions
            {
                KeyVaultUri = keyVaultUri,
                MicrosoftAppPassword = CloudConfigurationManager.GetSetting("MicrosoftAppPassword"),
                CosmosDBKey = CloudConfigurationManager.GetSetting("CosmosDBKey"),
                Key = CloudConfigurationManager.GetSetting("Key"),
            });

            builder.Register(c => new TelemetryClient(new TelemetryConfiguration(appInsightsInstrumentationKey)))
                .SingleInstance();

            builder.Register(c => new CertificateClient(new Uri(keyVaultUri), new DefaultAzureCredential()))
                .SingleInstance();

            builder.Register(c => new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential()))
                .SingleInstance();

            builder.RegisterType<SecretsProvider>().As<ISecretsProvider>()
                .SingleInstance();

            builder.RegisterType<BotFrameworkHttpAdapter>()
                .As<IBotFrameworkHttpAdapter>()
                .As<BotAdapter>()
                .SingleInstance();

            builder.RegisterType<ConversationHelper>()
                .SingleInstance();

            builder.RegisterType<IcebreakerBot>().As<IBot>()
                .SingleInstance();

            builder.RegisterType<MatchingService>().As<IMatchingService>()
                .SingleInstance();

            builder.RegisterType<IcebreakerBotDataProvider>().As<IBotDataProvider>()
                .SingleInstance();
        }
    }
}