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
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Data provider routines
    /// </summary>
    public class IcebreakerBotDataProvider
    {
        private static TelemetryClient telemetry = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
        private readonly Task initializeTask;
        private DocumentClient documentClient;
        private DocumentCollection teamsInstalledDocCol;
        private DocumentCollection usersOptInStatusDocCol;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotDataProvider"/> class.
        /// </summary>
        public IcebreakerBotDataProvider()
        {
            this.initializeTask = this.InitializeDatabaseAsync();
        }

        /// <summary>
        /// Save team installation status to store.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Updated team installation info</returns>
        public async Task<TeamInstallInfo> SaveTeamInstallStatus(TeamInstallInfo team, bool installed)
        {
            telemetry.TrackTrace("Hit the method - SaveTeamInstallStatus at: " + DateTime.Now.ToString());

            await this.initializeTask;

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                team);
            }
            else
            {
                // query first

                // Set some common query options
                FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

                var lookupQuery = this.documentClient.CreateDocumentQuery<TeamInstallInfo>(
                     UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                     .Where(t => t.TeamId == team.TeamId);

                var match = lookupQuery.ToList();

                if (match.Count > 0)
                {
                    var response = this.documentClient.DeleteDocumentAsync(match.First().SelfLink);
                }
            }

            return team;
        }

        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        public async Task<List<TeamInstallInfo>> GetInstalledTeams()
        {
            telemetry.TrackTrace("Hit the method - GetInstalledTeams at: " + DateTime.Now.ToString());

            await this.initializeTask;

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true };

            // Find matching activities
            try
            {
                var lookupQuery = this.documentClient.CreateDocumentQuery<TeamInstallInfo>(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions);

                var match = lookupQuery.ToList();
                return match;
            }
            catch (Exception ex)
            {
                telemetry.TrackTrace($"Hit a snag - {ex.InnerException} at: " + DateTime.Now.ToString());

                return null;
            }
        }

        /// <summary>
        /// Get the opt-in status of the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <returns>User information</returns>
        public async Task<UserInfo> GetUserOptInStatus(string tenantId, string userId)
        {
            telemetry.TrackTrace("Hit the GetUserOptInStatus method at: " + DateTime.Now.ToString());

            await this.initializeTask;

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            // Set some common query options
            FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true, PartitionKey = new PartitionKey("/tenantId") };

            // Find matching activities
            try
            {
                var lookupQuery = this.documentClient.CreateDocumentQuery<UserInfo>(
                        UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                        .Where(f => f.TenantId == tenantId && f.UserId == userId);

                var match = lookupQuery.ToList();
                return match.FirstOrDefault();
            }
            catch (Exception ex)
            {
                telemetry.TrackTrace($"Hit a snag - {ex.InnerException} at: " + DateTime.Now.ToString());
                return null;
            }
        }

        /// <summary>
        /// Set the opt-in status of the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <returns>Updated user information</returns>
        public async Task<UserInfo> SetUserOptInStatus(string tenantId, string userId, bool optedIn, string serviceUrl)
        {
            telemetry.TrackTrace("Hit the method - SetUserOptInStatus");

            await this.initializeTask;

            var obj = new UserInfo()
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };

            obj = await this.StoreUserOptInStatus(obj);

            Dictionary<string, string> setUserOptInProps = new Dictionary<string, string>()
            {
                { "tenantId", tenantId },
                { "userId", userId },
                { "optedIn", optedIn.ToString() },
                { "serviceUrl", serviceUrl }
            };

            telemetry.TrackEvent("SetUserOptInStatus", setUserOptInProps);

            return obj;
        }

        /// <summary>
        /// Stores the given pairup
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="user1Id">First user</param>
        /// <param name="user2Id">Second user</param>
        /// <returns>Tracking task</returns>
        public async Task StorePairup(string tenantId, string user1Id, string user2Id)
        {
            await this.initializeTask;

            var maxPairUpHistory = Convert.ToInt64(CloudConfigurationManager.GetSetting("MaxPairUpHistory"));

            var user1Info = await this.GetUserOptInStatus(tenantId, user1Id);
            var user2Info = await this.GetUserOptInStatus(tenantId, user2Id);

            user1Info.RecentPairUps.Add(user2Info);
            if (user1Info.RecentPairUps.Count >= maxPairUpHistory)
            {
                user1Info.RecentPairUps.RemoveAt(0);
            }

            telemetry.TrackTrace($"Having the PairUp stored for - {user1Id} inside of {tenantId}");
            await this.StoreUserOptInStatus(user1Info);

            user2Info.RecentPairUps.Add(user1Info);
            if (user2Info.RecentPairUps.Count >= maxPairUpHistory)
            {
                user2Info.RecentPairUps.RemoveAt(0);
            }

            telemetry.TrackTrace($"Having the PairUp stored for - {user2Id} inside of {tenantId}");
            await this.StoreUserOptInStatus(user2Info);
        }

        private async Task<UserInfo> StoreUserOptInStatus(UserInfo obj)
        {
            Dictionary<string, string> propDictionary = new Dictionary<string, string>
            {
                { "optedIn", obj.OptedIn.ToString() },
                { "userId", obj.UserId },
                { "tenantId", obj.TenantId }
            };

            telemetry.TrackEvent("StoreUserOptInStatus", propDictionary);

            await this.initializeTask;

            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var collectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");

            var existingDoc = await this.GetUserOptInStatus(obj.TenantId, obj.UserId);
            if (existingDoc != null)
            {
                // update
                var response = await this.documentClient.DeleteDocumentAsync(existingDoc.SelfLink);
            }
            else
            {
                // Insert
                var response = await this.documentClient.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(databaseName, collectionName),
                obj);
            }

            return obj;
        }

        private async Task InitializeDatabaseAsync()
        {
            var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
            var primaryKey = CloudConfigurationManager.GetSetting("CosmosDBKey");
            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");

            var teamsCollection = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");
            var usersCollection = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");
            var dbLink = endpointUrl + databaseName;

            this.documentClient = new DocumentClient(new Uri(endpointUrl), primaryKey);

            Database db = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
            if (db != null)
            {
                telemetry.TrackTrace($"Database: {db.Id} has been successfully created");
            }

            DocumentCollection teamsCollectionDef = new DocumentCollection
            {
                Id = teamsCollection
            };
            teamsCollectionDef.PartitionKey.Paths.Add("/teamId");

            DocumentCollection usersCollectionDef = new DocumentCollection
            {
                Id = usersCollection
            };
            usersCollectionDef.PartitionKey.Paths.Add("/tenantId");

            this.teamsInstalledDocCol = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), teamsCollectionDef, new RequestOptions { OfferThroughput = 400 });
            if (this.teamsInstalledDocCol != null)
            {
                telemetry.TrackTrace($"Teams collection {this.teamsInstalledDocCol.Id} was been created successfully");
            }

            this.usersOptInStatusDocCol = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), usersCollectionDef, new RequestOptions { OfferThroughput = 400 });
            if (this.usersOptInStatusDocCol != null)
            {
                telemetry.TrackTrace($"Users collection {this.usersOptInStatusDocCol.Id} have been created successfully");
            }
        }
    }
}