// <copyright file="IcebreakerBotDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using Microsoft.Azure.Documents.Linq;

    /// <summary>
    /// Data provider routines
    /// </summary>
    public class IcebreakerBotDataProvider : IBotDataProvider
    {
        // Request the minimum throughput by default
        private const int DefaultRequestThroughput = 400;

        private readonly TelemetryClient telemetryClient;
        private readonly Lazy<Task> initializeTask;
        private readonly ISecretsHelper secretsHelper;
        private DocumentClient documentClient;
        private Database database;
        private DocumentCollection teamsCollection;
        private DocumentCollection usersCollection;
        private DocumentCollection questionsCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBotDataProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">The telemetry client to use</param>
        /// <param name="secretsHelper">Secrets helper to fetch secrets</param>
        public IcebreakerBotDataProvider(TelemetryClient telemetryClient, ISecretsHelper secretsHelper)
        {
            this.telemetryClient = telemetryClient;
            this.secretsHelper = secretsHelper;
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
                await this.documentClient.UpsertDocumentAsync(this.teamsCollection.SelfLink, team);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.teamsCollection.Id, team.Id);
                await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(team.Id) });
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
        /// Get the stored information about given users
        /// </summary>
        /// <returns>User information</returns>
        public async Task<Dictionary<string, bool>> GetAllUsersOptInStatusAsync()
        {
            await this.EnsureInitializedAsync();

            try
            {
                var collectionLink = UriFactory.CreateDocumentCollectionUri(this.database.Id, this.usersCollection.Id);
                var query = this.documentClient.CreateDocumentQuery<UserInfo>(
                        collectionLink,
                        new FeedOptions
                        {
                            EnableCrossPartitionQuery = true,

                            // Fetch items in bulk according to DB engine capability
                            MaxItemCount = -1,

                            // Max partition to query at a time
                            MaxDegreeOfParallelism = -1,
                        })
                    .Select(u => new UserInfo { Id = u.Id, OptedIn = u.OptedIn })
                    .AsDocumentQuery();
                var usersOptInStatusLookup = new Dictionary<string, bool>();
                while (query.HasMoreResults)
                {
                    // Note that ExecuteNextAsync can return many records in each call
                    var responseBatch = await query.ExecuteNextAsync<UserInfo>();
                    foreach (var userInfo in responseBatch)
                    {
                        usersOptInStatusLookup.Add(userInfo.Id, userInfo.OptedIn);
                    }
                }

                return usersOptInStatusLookup;
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
                ServiceUrl = serviceUrl,
            };
            await this.documentClient.UpsertDocumentAsync(this.usersCollection.SelfLink, userInfo);
        }

        /// <summary>
        /// Get all saved Questions.
        /// </summary>
        /// /// <param name="language">The language to get the questions in</param>
        /// <returns>List of Questions</returns>
        public async Task<string[]> GetQuestionsAsync(string language)
        {
            await this.EnsureInitializedAsync();

            try
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.questionsCollection.Id, language);
                Question question = await this.documentClient.ReadDocumentAsync<Question>(documentUri, new RequestOptions { PartitionKey = new PartitionKey(language) });
                return question.Questions;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex.InnerException);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task SetQuestionsAsync(string language, string[] questions)
        {
            await this.EnsureInitializedAsync();

            var question = new Question
            {
                Language = language,
                Questions = questions,
            };
            await this.documentClient.UpsertDocumentAsync(this.questionsCollection.SelfLink, question);
        }

        /// <summary>
        /// Initializes the database connection.
        /// </summary>
        /// <returns>Tracking task</returns>
        private async Task InitializeAsync()
        {
            this.telemetryClient.TrackTrace("Initializing data store");

            var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
            var teamsCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionTeams");
            var usersCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionUsers");
            var questionsCollectionName = CloudConfigurationManager.GetSetting("CosmosCollectionQuestions");

            this.documentClient = new DocumentClient(new Uri(endpointUrl), this.secretsHelper.CosmosDBKey);

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

            requestOptions = useSharedOffer ? null : requestOptions;

            // Get a reference to the Teams collection, creating it if needed
            this.teamsCollection = await this.CreateDocumentCollectionIfNotExistsAsync(teamsCollectionName, requestOptions);
            this.usersCollection = await this.CreateDocumentCollectionIfNotExistsAsync(usersCollectionName, requestOptions);
            this.questionsCollection = await this.CreateDocumentCollectionIfNotExistsAsync(questionsCollectionName, requestOptions);

            this.telemetryClient.TrackTrace("Data store initialized");
        }

        private async Task<ResourceResponse<DocumentCollection>> CreateDocumentCollectionIfNotExistsAsync(string collectionname, RequestOptions requestOptions)
        {
            var collectionDefinition = new DocumentCollection
            {
                Id = collectionname,
            };
            collectionDefinition.PartitionKey.Paths.Add("/id");
            return await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, collectionDefinition, requestOptions);
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}