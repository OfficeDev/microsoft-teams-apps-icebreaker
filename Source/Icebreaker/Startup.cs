namespace Icebreaker
{
    using System;
    using System.Globalization;
    using System.Linq;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Secrets;
    using Icebreaker.BackgroundTasks;
    using Icebreaker.Bot;
    using Icebreaker.Helpers;
    using Icebreaker.Interfaces;
    using Icebreaker.Localization;
    using Icebreaker.Services;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.Core;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Identity.Web;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// Startup
    /// </summary>
    public class Startup
    {
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The environment provided configuration</param>
        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Setting configuration services
        /// </summary>
        /// <param name="services">services</param>
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLocalization(this.configuration);
            services.AddApplicationInsightsTelemetry();

            // Authentication - Identity.Web, Graph SDK
            services
                .AddMicrosoftIdentityWebApiAuthentication(this.configuration);

        

            var appInsightsInstrumentationKey = this.configuration.GetValue<string>("APPINSIGHTS_INSTRUMENTATIONKEY");
            var keyVaultUri = this.configuration.GetValue<string>("KeyVaultUri");

            var telemetryClient = new TelemetryClient(new TelemetryConfiguration(appInsightsInstrumentationKey));
            services.AddSingleton(telemetryClient);

            var credentialProvider = new SimpleCredentialProvider(
                                    this.configuration.GetValue<string>("MicrosoftAppId"),
                                    this.configuration.GetValue<string>("MicrosoftAppPassword"));
            services.AddSingleton<ICredentialProvider>(credentialProvider);

            var secretOptions = new SecretOptions
            {
                KeyVaultUri = keyVaultUri,
                MicrosoftAppPassword = this.configuration.GetValue<string>("MicrosoftAppPassword"),
                CosmosDBKey = this.configuration.GetValue<string>("CosmosDBKey"),
                Key = this.configuration.GetValue<string>("Key"),
            };
            services.AddSingleton(secretOptions);

            var certClient = new CertificateClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            services.AddSingleton(certClient);

            var secretClient = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
            services.AddSingleton(secretClient);

            // Bot dependencies
            var appSettings = new AppSettings
            {
                IsTesting = Convert.ToBoolean(this.configuration.GetValue<string>("Testing")),
                DisableTenantFilter = Convert.ToBoolean(this.configuration.GetValue<string>("DisableTenantFilter"), CultureInfo.InvariantCulture),
                AllowedTenantIds = this.configuration.GetValue<string>("AllowedTenants")?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)?.Select(p => p.Trim())
                .ToHashSet(),
                BotDisplayName = this.configuration.GetValue<string>("BotDisplayName"),
                BotCertName = this.configuration.GetValue<string>("BotCertificateName"),
                MicrosoftAppId = this.configuration.GetValue<string>("MicrosoftAppId"),
                AppBaseDomain = this.configuration.GetValue<string>("AppBaseDomain"),
                CosmosDBEndpointUrl = this.configuration.GetValue<string>("CosmosDBEndpointUrl"),
                CosmosDBDatabaseName = this.configuration.GetValue<string>("CosmosDBDatabaseName"),
                CosmosCollectionTeams = this.configuration.GetValue<string>("CosmosCollectionTeams"),
                CosmosCollectionUsers = this.configuration.GetValue<string>("CosmosCollectionUsers"),
                AppInsightsInstrumentationKey = appInsightsInstrumentationKey,
                DefaultCulture = this.configuration.GetValue<string>("DefaultCulture"),
                MaxPairUpsPerTeam = int.Parse(this.configuration.GetValue<string>("MaxPairUpsPerTeam")),
            };
            services.AddSingleton<IAppSettings>(appSettings);
            services.AddTransient<ISecretsProvider, SecretsProvider>();

            services.AddTransient<IcebreakerBot>();
            services.AddTransient<IceBreakerBotMiddleware>();
            services.AddSingleton<BotHttpAdapter>();
            services.AddSingleton<BotFrameworkHttpAdapter>();
            services.AddTransient<IBot, IcebreakerBot>();

            services.AddTransient<ConversationHelper>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            services.AddHostedService<BackgroundQueueService>();

            // Controllers
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                   options.SerializerSettings.Converters
                   .Add(new StringEnumConverter(new DefaultNamingStrategy(), false)));
            services.AddTransient<IMatchingService, MatchingService>();
            services.AddTransient<IBotDataProvider, IcebreakerBotDataProvider>();

        }

        /// <summary>
        /// Configure
        /// </summary>
        /// <param name="app">app</param>
        /// <param name="env">env</param>
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
