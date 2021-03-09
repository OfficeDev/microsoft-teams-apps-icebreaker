// <copyright file="WebApiConfig.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker
{
    using System.Web.Http;
    using System.Web.Http.Dependencies;
    using Autofac;
    using Autofac.Integration.WebApi;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Web API configuration
    /// </summary>
    public static class WebApiConfig
    {
        /// <summary>
        /// Configures API settings
        /// </summary>
        /// <param name="config">The <see cref="HttpConfiguration"/> to configure</param>
        public static void Register(HttpConfiguration config)
        {
            // Json settings
            config.Formatters.JsonFormatter.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            config.Formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
            };

            // Web API configuration and services
            config.DependencyResolver = GetDependencyResolver();

            // Culture specific settings
            config.MessageHandlers.Add(new CultureSpecificMessageHandler());

            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });
        }

        /// <summary>
        /// Build container and return dependency resolver
        /// </summary>
        /// <returns>Dependency resolver</returns>
        private static IDependencyResolver GetDependencyResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new IcebreakerModule());
            var container = builder.Build();
            var resolver = new AutofacWebApiDependencyResolver(container);
            return resolver;
        }
    }
}
