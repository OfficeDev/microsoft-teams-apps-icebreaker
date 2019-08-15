//----------------------------------------------------------------------------------------------
// <copyright file="RouteConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System.Web.Mvc;
    using System.Web.Routing;

    /// <summary>
    /// Route Config class
    /// </summary>
    public class RouteConfig
    {
        /// <summary>
        /// RegisterRoutes method
        /// </summary>
        /// <param name="routes">Route Collection</param>
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