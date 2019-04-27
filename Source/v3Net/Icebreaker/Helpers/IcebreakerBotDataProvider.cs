//----------------------------------------------------------------------------------------------
// <copyright file="IcebreakerBotDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// Data provider routines
    /// </summary>
    public class IcebreakerBotDataProvider
    {
        // Request the minimum throughput by default
        private const int DefaultRequestThroughput = 400;

        private static TelemetryClient telemetry = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));

        private readonly Lazy<Task> initializeTask;
        private DocumentClient documentClient;
        private Database database;
        private DocumentCollection teamsCollection;
        private DocumentCollection usersCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotDataProvider"/> class.
        /// </summary>
        public IcebreakerBotDataProvider()
        {
            this.initializeTask = new Lazy<Task>(this.InitializeAsync);
        }

        /// <summary>
        /// Updates team installation status in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        public async Task UpdateTeamInstallStatusAsync(TeamInstallInfo team, bool installed)
        {
            telemetry.TrackTrace("Hit the method SaveTeamInstallStatus");

            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.teamsCollection.SelfLink, team);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, team.Id);
                var response = await this.documentClient.DeleteDocumentAsync(documentUri);
            }
        }

        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        public async Task<List<TeamInstallInfo>> GetInstalledTeamsAsync()
        {
            telemetry.TrackTrace("Hit the method GetInstalledTeams");

            await this.EnsureInitializedAsync();

            var installedTeams = new List<TeamInstallInfo>();

            try
            {
                var lookupQuery = this.documentClient
                    .CreateDocumentQuery<TeamInstallInfo>(this.teamsCollection.SelfLink)
                    .AsDocumentQuery();
                while (lookupQuery.HasMoreResults)
                {
                    var response = await lookupQuery.ExecuteNextAsync<TeamInstallInfo>();
                    installedTeams.AddRange(response);
                }
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex.InnerException);
            }

            return installedTeams;
        }

        /// <summary>
        /// Returns the team that the bot has been installed to
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The team id</param>
        /// <returns>Team that the bot is installed to</returns>
        public async Task<TeamInstallInfo> GetInstalledTeamAsync(string tenantId, string teamId)
        {
            telemetry.TrackTrace("Hit the GetInstalledTeam method");

            await this.EnsureInitializedAsync();

            // Get team install info
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, teamId);
                return await this.documentClient.ReadDocumentAsync<TeamInstallInfo>(documentUri);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <summary>
        /// Get the stored information about the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <returns>User information</returns>
        public async Task<UserInfo> GetUserInfoAsync(string tenantId, string userId)
        {
            telemetry.TrackTrace("Hit the GetUserInfo method");

            await this.EnsureInitializedAsync();

            // Find matching activities
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.usersCollection.Id, userId);
                return await this.documentClient.ReadDocumentAsync<UserInfo>(documentUri);
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <summary>
        /// Set the user info for the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <returns>Tracking task</returns>
        public async Task SetUserInfoAsync(string tenantId, string userId, bool optedIn, string serviceUrl)
        {
            telemetry.TrackTrace("Hit the method - SetUserInfoAsync");

            await this.EnsureInitializedAsync();

            var obj = new UserInfo()
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };

            await this.StoreUserInfoAsync(obj);

            Dictionary<string, string> setUserOptInProps = new Dictionary<string, string>()
            {
                { "tenantId", tenantId },
                { "userId", userId },
                { "optedIn", optedIn.ToString() },
                { "serviceUrl", serviceUrl }
            };

            telemetry.TrackEvent("SetUserInfoAsync", setUserOptInProps);
        }

        /// <summary>
        /// Stores the given pairup
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="user1Id">First user</param>
        /// <param name="user2Id">Second user</param>
        /// <returns>Tracking task</returns>
        public async Task StorePairupAsync(string tenantId, string user1Id, string user2Id)
        {
            await this.EnsureInitializedAsync();

            var maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));

            var user1Info = await this.GetUserInfoAsync(tenantId, user1Id);
            var user2Info = await this.GetUserInfoAsync(tenantId, user2Id);

            user1Info.RecentPairUps.Add(user2Info);
            if (user1Info.RecentPairUps.Count >= maxPairUpHistory)
            {
                user1Info.RecentPairUps.RemoveAt(0);
            }

            telemetry.TrackTrace($"Having the PairUp stored for - {user1Id} inside of {tenantId}");
            await this.StoreUserInfoAsync(user1Info);

            user2Info.RecentPairUps.Add(user1Info);
            if (user2Info.RecentPairUps.Count >= maxPairUpHistory)
            {
                user2Info.RecentPairUps.RemoveAt(0);
            }

            telemetry.TrackTrace($"Having the PairUp stored for - {user2Id} inside of {tenantId}");
            await this.StoreUserInfoAsync(user2Info);
        }

        /// <summary>
        /// Initializes the database connection.
        /// </summary>
        /// <returns>Tracking task</returns>
        private async Task InitializeAsync()
        {
            var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
            var primaryKey = CloudConfigurationManager.GetSetting("CosmosDBKey");
            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var teamsCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");
            var usersCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            this.documentClient = new DocumentClient(new Uri(endpointUrl), primaryKey);

            // Create the database if needed
            this.database = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
            if (this.database != null)
            {
                telemetry.TrackTrace($"Reference to database {this.database.Id} obtained successfully");
            }

            var requestOptions = new RequestOptions { OfferThroughput = DefaultRequestThroughput };

            // Get a reference to the Teams collection, creating it if needed
            var teamsCollectionDefinition = new DocumentCollection
            {
                Id = teamsCollectionName,
            };

            this.teamsCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, teamsCollectionDefinition, requestOptions);
            if (this.teamsCollection != null)
            {
                telemetry.TrackTrace($"Reference to Teams collection database {this.teamsCollection.Id} obtained successfully");
            }

            // Get a reference to the Users collection, creating it if needed
            var usersCollectionDefinition = new DocumentCollection
            {
                Id = usersCollectionName
            };

            this.usersCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, usersCollectionDefinition, requestOptions);
            if (this.usersCollection != null)
            {
                telemetry.TrackTrace($"Reference to Users collection database {this.usersCollection.Id} obtained successfully");
            }
        }

        private Task EnsureInitializedAsync()
        {
            return this.initializeTask.Value;
        }

        private async Task StoreUserInfoAsync(UserInfo obj)
        {
            await this.documentClient.UpsertDocumentAsync(this.usersCollection.SelfLink, obj);
        }
    }
}