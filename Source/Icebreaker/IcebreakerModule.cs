// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.WebApi;
    using Icebreaker.Bot;
    using Icebreaker.Helpers;
    using Icebreaker.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.BotFramework;
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

            // The ConfigurationCredentialProvider will retrieve the MicrosoftAppId and
            // MicrosoftAppPassword from Web.config
            builder.RegisterType<ConfigurationCredentialProvider>().As<ICredentialProvider>().SingleInstance();

            builder.Register(c =>
            {
                return new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
            }).SingleInstance();

            builder.Register(c => new MicrosoftAppCredentials(CloudConfigurationManager.GetSetting("MicrosoftAppId"), CloudConfigurationManager.GetSetting("MicrosoftAppPassword")))
                .SingleInstance();

            builder.RegisterType<BotFrameworkHttpAdapter>().As<IBotFrameworkHttpAdapter>()
                .SingleInstance();

            builder.RegisterType<ConversationHelper>()
                .SingleInstance();

            builder.RegisterType<IcebreakerBot>().As<IBot>()
                .SingleInstance();

            builder.RegisterType<MatchingService>()
                .SingleInstance();

            builder.RegisterType<IcebreakerBotDataProvider>()
                .SingleInstance();
        }
    }
}