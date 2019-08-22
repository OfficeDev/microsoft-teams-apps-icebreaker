// <copyright file="AutofacConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using Autofac;
    using Autofac.Integration.Mvc;
    using Autofac.Integration.WebApi;

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

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            builder.RegisterControllers(Assembly.GetExecutingAssembly());
        }
    }
}