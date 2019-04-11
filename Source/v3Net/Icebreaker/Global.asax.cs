// <copyright file="Global.asax.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Web.Http;

#pragma warning disable SA1649 // File name must match first type name
    public class WebApiApplication : System.Web.HttpApplication
#pragma warning restore SA1649 // File name must match first type name
    {
#pragma warning disable SA1600 // Elements must be documented
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        protected void Application_Start()
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
#pragma warning restore SA1600 // Elements must be documented
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
