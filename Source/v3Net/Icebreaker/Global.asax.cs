// <copyright file="Global.asax.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Reflection;
    using System.Web.Http;
    using Autofac;
    using Autofac.Integration.WebApi;
    using Microsoft.Bot.Builder.Dialogs;

#pragma warning disable SA1649 // File name must match first type name
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable SA1600 // Elements must be documented

    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            Conversation.UpdateContainer(
               builder =>
               {
                   builder.RegisterModule(new IcebreakerModule());

                   builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
                   builder.RegisterWebApiFilterProvider(GlobalConfiguration.Configuration);
               });
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
