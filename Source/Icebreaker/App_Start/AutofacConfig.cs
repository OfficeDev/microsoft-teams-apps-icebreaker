// <copyright file="AutofacConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.Mvc;
    using Autofac.Integration.WebApi;
    using Icebreaker.Bots;
    using Icebreaker.Helpers;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.BotFramework;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Establishing the necessary dependencies.
    /// </summary>
    public class AutofacConfig
    {
        /// <summary>
        /// Method to establish all dependencies.
        /// </summary>
        public static void RegisterDependencies()
        {
            var builder = new ContainerBuilder();

            builder.Register(c => new IcebreakerBot(
                c.Resolve<TelemetryClient>(),
                c.Resolve<IcebreakerBotDataProvider>(),
                c.Resolve<MicrosoftAppCredentials>(),
                CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                CloudConfigurationManager.GetSetting("BotDisplayName"))).SingleInstance();

            builder.Register(c => new AdapterWithErrorHandler(c.Resolve<ICredentialProvider>()))
                .SingleInstance();

            builder.Register(c =>
            {
                return new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
            }).SingleInstance();

            builder.Register(c =>
            {
                return new MicrosoftAppCredentials(CloudConfigurationManager.GetSetting("MicrosoftAppId"), CloudConfigurationManager.GetSetting("MicrosoftAppPassword"));
            }).SingleInstance();

            builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<ConfigurationCredentialProvider>().As<ICredentialProvider>().SingleInstance();
            builder.RegisterType<AdapterWithErrorHandler>().As<IBotFrameworkHttpAdapter>();

            builder.RegisterModule(new IcebreakerModule());
            builder.RegisterType<IcebreakerBot>().As<IBot>().SingleInstance();

            var container = builder.Build();
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}