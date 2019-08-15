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
    using Microsoft.ApplicationInsights.DataContracts;
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

        private readonly TelemetryClient telemetryClient;
        private readonly Lazy<Task> initializeTask;
        private DocumentClient documentClient;
        private Database database;
        private DocumentCollection teamsCollection;
        private DocumentCollection usersCollection;
        private DocumentCollection pairUpusersCollections;
        private DocumentCollection feedbackCollections;
        private DocumentCollection imageCollections;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotDataProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public IcebreakerBotDataProvider(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync());
        }

        /// <summary>
        /// Updates team installation status in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        public async Task UpdateTeamInstallStatusAsync(TeamInstallInfo team, bool installed)
        {
            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.teamsCollection.SelfLink, team);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, team.Id);
                var response = await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(team.Id) });
            }
        }

        /// <summary>
        /// PairupUsers
        /// </summary>
        /// <param name="pairupUsers">Collection of users to be paired</param>
        /// <param name="installed">App is installed or not</param>
        /// <returns>insert data to db</returns>
        public async Task UpdatePairupUsersAsync(PairupUsers pairupUsers, bool installed)
        {
            // pairUpusersCollections.Id = "";
            var random = new Random().Next(1, 999999);
            installed = true;
            pairupUsers.PairupId = random.ToString();
            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.pairUpusersCollections.SelfLink, pairupUsers);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.pairUpusersCollections.Id, pairupUsers.PairupId);
                var response = await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(pairupUsers.Id) });
            }
        }

        /// <summary>
        /// Updates feedback info in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="feedback">The feedback info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        public async Task UpdateFeedbackInfoAsync(FeedbackInfo feedback, bool installed)
        {
            installed = true;
            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.feedbackCollections.SelfLink, feedback);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.feedbackCollections.Id, feedback.FeedbackId);
                var response = await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(feedback.Id) });
            }
        }

        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        public async Task<IList<TeamInstallInfo>> GetInstalledTeamsAsync()
        {
            await this.EnsureInitializedAsync();

            var installedTeams = new List<TeamInstallInfo>();

            try
            {
                using (var lookupQuery = this.documentClient
                    .CreateDocumentQuery<TeamInstallInfo>(this.teamsCollection.SelfLink, new FeedOptions { EnableCrossPartitionQuery = true })
                    .AsDocumentQuery())
                {
                    while (lookupQuery.HasMoreResults)
                    {
                        var response = await lookupQuery.ExecuteNextAsync<TeamInstallInfo>();
                        installedTeams.AddRange(response);
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
            }

            return installedTeams;
        }

        /// <summary>
        /// Returns the team that the bot has been installed to
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <returns>Team that the bot is installed to</returns>
        public async Task<TeamInstallInfo> GetInstalledTeamAsync(string teamId)
        {
            await this.EnsureInitializedAsync();

            // Get team install info
            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, teamId);
                return await this.documentClient.ReadDocumentAsync<TeamInstallInfo>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(teamId) });
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <summary>
        /// Returns the pairup users data
        /// </summary>
        /// <returns>List of pairup users</returns>
        public async Task<IList<PairupUsers>> GetPairUpUsersAsync()
        {
            await this.EnsureInitializedAsync();
            var pairupUsers = new List<PairupUsers>();
            try
            {
                var query = new SqlQuerySpec(
             "SELECT * FROM c WHERE c.Ispaired = @Ispairedvalue",
             new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@Ispairedvalue", Value = true } }));
                using (var lookupQuery = this.documentClient
                    .CreateDocumentQuery<PairupUsers>(this.pairUpusersCollections.SelfLink, query, new FeedOptions { EnableCrossPartitionQuery = true })
                    .AsDocumentQuery())
                {
                    while (lookupQuery.HasMoreResults)
                    {
                        var response = await lookupQuery.ExecuteNextAsync<PairupUsers>();
                        pairupUsers.AddRange(response);
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
            }

            return pairupUsers;
        }

        /// <summary>
        /// Get the stored information about the given user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User information</returns>
        public async Task<UserInfo> GetUserInfoAsync(string userId)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.usersCollection.Id, userId);
                return await this.documentClient.ReadDocumentAsync<UserInfo>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(userId) });
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
            await this.EnsureInitializedAsync();

            var userInfo = new UserInfo
            {
                TenantId = tenantId,
                UserId = userId,
                OptedIn = optedIn,
                ServiceUrl = serviceUrl
            };
            await this.documentClient.UpsertDocumentAsync(this.usersCollection.SelfLink, userInfo);
        }

        /// <summary>
        /// Initializes the database connection.
        /// </summary>
        /// <returns>Tracking task</returns>
        private async Task InitializeAsync()
        {
            this.telemetryClient.TrackTrace("Initializing data store");

            var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
            var primaryKey = CloudConfigurationManager.GetSetting("CosmosDBKey");
            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var teamsCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");
            var usersCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");
            var pairupUsersCollectionName = CloudConfigurationManager.GetSetting("PairupUsersCollections");
            var feedbackInfoCollectionName = CloudConfigurationManager.GetSetting("FeedbackInfoCollections");
            var imageInfoCollectionName = CloudConfigurationManager.GetSetting("ImageInfoCollections");
            this.documentClient = new DocumentClient(new Uri(endpointUrl), primaryKey);
            var requestOptions = new RequestOptions { OfferThroughput = DefaultRequestThroughput };
            bool useSharedOffer = true;

            // Create the database if needed
            try
            {
                this.database = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName }, requestOptions);
            }
            catch (DocumentClientException ex)
            {
                if (ex.Error?.Message?.Contains("SharedOffer is Disabled") ?? false)
                {
                    this.telemetryClient.TrackTrace("Database shared offer is disabled for the account, will provision throughput at container level", SeverityLevel.Information);
                    useSharedOffer = false;

                    this.database = await this.documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
                }
                else
                {
                    throw;
                }
            }

            // Get a reference to the Teams collection, creating it if needed
            var teamsCollectionDefinition = new DocumentCollection
            {
                Id = teamsCollectionName,
            };
            teamsCollectionDefinition.PartitionKey.Paths.Add("/id");

            this.teamsCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, teamsCollectionDefinition, useSharedOffer ? null : requestOptions);

            // Get a reference to the Users collection, creating it if needed
            var usersCollectionDefinition = new DocumentCollection
            {
                Id = usersCollectionName
            };
            usersCollectionDefinition.PartitionKey.Paths.Add("/id");

            var pairUpUsersCollectionDefinition = new DocumentCollection
            {
                Id = pairupUsersCollectionName,
            };
            pairUpUsersCollectionDefinition.PartitionKey.Paths.Add("/id");

            this.usersCollection = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, usersCollectionDefinition, useSharedOffer ? null : requestOptions);
            this.pairUpusersCollections = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, pairUpUsersCollectionDefinition, useSharedOffer ? null : requestOptions);

            var feedbackInfoCollectionDefinition = new DocumentCollection
            {
                Id = feedbackInfoCollectionName,
            };
            feedbackInfoCollectionDefinition.PartitionKey.Paths.Add("/id");
            this.feedbackCollections = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, feedbackInfoCollectionDefinition, useSharedOffer ? null : requestOptions);

            var imageInfoCollectionDefinition = new DocumentCollection
            {
                Id = imageInfoCollectionName,
            };
            imageInfoCollectionDefinition.PartitionKey.Paths.Add("/id");
            this.imageCollections = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, imageInfoCollectionDefinition, useSharedOffer ? null : requestOptions);
            this.telemetryClient.TrackTrace("Data store initialized");
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}