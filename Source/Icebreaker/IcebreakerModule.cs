// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.WebApi;
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

            builder.RegisterType<SecretsHelper>().As<ISecretsHelper>()
                .SingleInstance();

            // ICredentialProvider is required for AD queries
            builder.Register(c => new SimpleCredentialProvider(
                    CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                    c.Resolve<ISecretsHelper>().MicrosoftAppPassword))
                .As<ICredentialProvider>()
                .SingleInstance();

            builder.Register(c =>
            {
                return new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
            }).SingleInstance();

            builder.Register(c => new MicrosoftAppCredentials(
                    CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                    c.Resolve<ISecretsHelper>().MicrosoftAppPassword))
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