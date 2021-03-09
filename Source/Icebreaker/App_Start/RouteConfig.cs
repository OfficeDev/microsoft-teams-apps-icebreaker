// <copyright file="RouteConfig.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker
{
    using System.Web.Mvc;
    using System.Web.Routing;

    /// <summary>
    /// Route configuration for MVC
    /// </summary>
    public class RouteConfig
    {
        /// <summary>
        /// Register all route rules
        /// </summary>
        /// <param name="routes">Route collection</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { action = "Index", id = UrlParameter.Optional });
        }
    }
}