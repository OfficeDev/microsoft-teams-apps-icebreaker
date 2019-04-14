// <copyright file="IcebreakerModule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using Autofac;

    /// <summary>
    /// Autofac Module
    /// </summary>
    public class IcebreakerModule : Module
    {
        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
        }
    }
}