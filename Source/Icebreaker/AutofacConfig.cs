// <copyright file="AutofacConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using System.Web.Http;
    using System.Web.Mvc;
    using Autofac;
    using Autofac.Integration.Mvc;
    using Autofac.Integration.WebApi;
    using Icebreaker.Bots;
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

            // builder.RegisterControllers(Assembly.GetExecutingAssembly());
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterType<ConfigurationCredentialProvider>().As<ICredentialProvider>().SingleInstance();
            builder.RegisterType<IBotFrameworkHttpAdapter>();
            builder.RegisterType<IcebreakerBot>().As<IBot>();

            var container = builder.Build();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
            GlobalConfiguration.Configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}