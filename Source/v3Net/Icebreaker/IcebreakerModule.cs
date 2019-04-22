// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using Autofac;
    using Icebreaker.Helpers;

    /// <summary>
    /// Autofac Module
    /// </summary>
    public class IcebreakerModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.RegisterType<IcebreakerBot>()
                .SingleInstance();

            builder.Register(async (c) =>
            {
                var provider = new IcebreakerBotDataProvider();
                await provider.InitializeAsync();
                return provider;
            }).SingleInstance();
        }
    }
}