// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using Autofac;
    using Icebreaker.Bots;
    using Icebreaker.Helpers;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;

    /// <summary>
    /// Autofac Module
    /// </summary>
    public class IcebreakerModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Register(c =>
            {
                return new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
            }).SingleInstance();

            builder.RegisterType<IcebreakerBot>().As<IBot>();

            builder.RegisterType<IcebreakerBotDataProvider>()
                .SingleInstance();
        }
    }
}