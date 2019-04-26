//----------------------------------------------------------------------------------------------
// <copyright file="IcebreakerBotDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Data provider routines
    /// </summary>
    public class IcebreakerBotDataProvider
    {
        // Request the minimum throughput by default
        private const int DefaultRequestThroughput = 400;

        private readonly TelemetryClient telemetryClient;
        private readonly Lazy<Task> initializeTask;
        private readonly long maxPairUpHistory;
        private DocumentClient documentClient;
        private DocumentCollection teamsCollection;
        private DocumentCollection usersCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotDataProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public IcebreakerBotDataProvider(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync());
            this.maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));
        }

        /// <summary>
        /// Updates team installation status in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        public async Task UpdateTeamInstallStatusAsync(TeamInstallInfo team, bool installed)
        {
            this.telemetryClient.TrackTrace("Hit the method SaveTeamInstallStatus");

            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.teamsCollection.SelfLink, team);
            }
            else
            {
                var partitionKey = new PartitionKey(team.TeamId);

                var lookupQuery = this.documentClient.CreateDocumentQuery<TeamInstallInfo>(
                    this.teamsCollection.SelfLink, new FeedOptions { MaxItemCount = -1, PartitionKey = partitionKey })
                    .Where(t => t.TeamId == team.TeamId);

                var match = lookupQuery.ToList();
                if (match.Count > 0)
                {
                    var response = this.documentClient.DeleteDocumentAsync(match.First().SelfLink, new RequestOptions { PartitionKey = partitionKey });
                }
            }
        }

        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        public async Task<IList<TeamInstallInfo>> GetInstalledTeamsAsync()
        {
            this.telemetryClient.TrackTrace("Hit the method GetInstalledTeams");

            await this.EnsureInitializedAsync();

            // Find matching activities
            try
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };
                var lookupQuery = this.documentClient.CreateDocumentQuery<TeamInstallInfo>(this.teamsCollection.SelfLink, queryOptions);
                var match = lookupQuery.ToList();
                return match;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);

                // Return no teams if we hit an error fetching
                return new List<TeamInstallInfo>();
            }
        }

        /// <summary>
        /// Returns the team that the bot has been installed to
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The team id</param>
        /// <returns>Team that the bot is installed to</returns>
        public async Task<TeamInstallInfo> GetTeamInstallInfoAsync(string tenantId, string teamId)
        {
            this.telemetryClient.TrackTrace("Hit the GetInstalledTeam method");

            await this.EnsureInitializedAsync();

            // Get team install info
            try
            {
                var queryOptions = new FeedOptions { MaxItemCount = -1, PartitionKey = new PartitionKey(teamId) };
                var results = this.documentClient.CreateDocumentQuery<TeamInstallInfo>(this.teamsCollection.SelfLink, queryOptions)
                    .Where(f => f.TenantId == tenantId && f.TeamId == teamId);
                var match = results.ToList();
                return match.FirstOrDefault();
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
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
            this.telemetryClient.TrackTrace("Hit the GetUserInfo method");

            await this.EnsureInitializedAsync();

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, PartitionKey = new PartitionKey(tenantId) };

            // Find matching activities
            try
            {
                var lookupQuery = this.documentClient.CreateDocumentQuery<UserInfo>(this.usersCollection.SelfLink, queryOptions)
                    .Where(f => f.TenantId == tenantId && f.UserId == userId);
                var match = lookupQuery.ToList();
                return match.FirstOrDefault();
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
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
            this.telemetryClient.TrackTrace("Hit the method - SetUserInfoAsync");

            await this.EnsureInitializedAsync();

            var obj = new UserInfo()
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };
            await this.StoreUserInfoAsync(obj);
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

            var user1Info = await this.GetUserInfoAsync(tenantId, user1Id);
            var user2Info = await this.GetUserInfoAsync(tenantId, user2Id);

            user1Info.RecentPairUps.Add(user2Info);
            if (user1Info.RecentPairUps.Count >= this.maxPairUpHistory)
            {
                user1Info.RecentPairUps.RemoveAt(0);
            }

            this.telemetryClient.TrackTrace($"Having the PairUp stored for - {user1Id} inside of {tenantId}");
            await this.StoreUserInfoAsync(user1Info);

            user2Info.RecentPairUps.Add(user1Info);
            if (user2Info.RecentPairUps.Count >= this.maxPairUpHistory)
            {
                user2Info.RecentPairUps.RemoveAt(0);
            }

            this.telemetryClient.TrackTrace($"Having the PairUp stored for - {user2Id} inside of {tenantId}");
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
            Database db = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
            if (db != null)
            {
                this.telemetryClient.TrackTrace($"Reference to database {db.Id} obtained successfully");
            }

            var requestOptions = new RequestOptions { OfferThroughput = DefaultRequestThroughput };

            // Get a reference to the Teams collection, creating it if needed
            var teamsCollectionDefinition = new DocumentCollection
            {
                Id = teamsCollectionName,
            };
            teamsCollectionDefinition.PartitionKey.Paths.Add("/teamId");

            this.teamsCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(db.SelfLink, teamsCollectionDefinition, requestOptions);
            if (this.teamsCollection != null)
            {
                this.telemetryClient.TrackTrace($"Reference to Teams collection database {this.teamsCollection.Id} obtained successfully");
            }

            // Get a reference to the Users collection, creating it if needed
            var usersCollectionDefinition = new DocumentCollection
            {
                Id = usersCollectionName
            };
            usersCollectionDefinition.PartitionKey.Paths.Add("/tenantId");

            this.usersCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(db.SelfLink, usersCollectionDefinition, requestOptions);
            if (this.usersCollection != null)
            {
                this.telemetryClient.TrackTrace($"Reference to Users collection database {this.usersCollection.Id} obtained successfully");
            }
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }

        private async Task StoreUserInfoAsync(UserInfo obj)
        {
            await this.documentClient.UpsertDocumentAsync(this.usersCollection.SelfLink, obj);
        }
    }
}