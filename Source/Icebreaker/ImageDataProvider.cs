//----------------------------------------------------------------------------------------------
// <copyright file="ImageDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System;
    using System.Threading.Tasks;
    using Icebreaker.Helpers;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

    /// <summary>
    /// Image Data provider class
    /// </summary>
    public class ImageDataProvider
    {
        private const int DefaultRequestThroughput = 400;
        private readonly TelemetryClient telemetryClient;
        private readonly Lazy<Task> initializeTask;
        private DocumentClient documentClient;
        private Database database;
        private DocumentCollection imageCollections;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageDataProvider"/> class.
        /// constructor of ImageDataProvider
        /// </summary>
        public ImageDataProvider()
        {
            // this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync());
        }

        /// <summary>
        /// UpdateImageInfoAsync
        /// </summary>
        /// <param name="imageInfo"> Image Info</param>
        /// <param name="installed">App is installed or not</param>
        /// <returns>Image info values</returns>
        public async Task UpdateImageInfoAsync(ImageInfo imageInfo, bool installed)
        {
            installed = true;
            await this.EnsureInitializedAsync();

            if (installed)
            {
                var response = await this.documentClient.UpsertDocumentAsync(this.imageCollections.SelfLink, imageInfo);
            }
            else
            {
                var documentUri = UriFactory.CreateDocumentUri(this.database.Id, this.imageCollections.Id, imageInfo.ImageId);
                var response = await this.documentClient.DeleteDocumentAsync(documentUri, new RequestOptions { PartitionKey = new PartitionKey(imageInfo.Id) });
            }
        }

        /// <summary>
        /// Initializing connection to cosmos DB
        /// </summary>
        /// <returns>Task</returns>
        private async Task InitializeAsync()
        {
            var endpointUrl = CloudConfigurationManager.GetSetting("CosmosDBEndpointUrl");
            var primaryKey = CloudConfigurationManager.GetSetting("CosmosDBKey");
            var databaseName = CloudConfigurationManager.GetSetting("CosmosDBDatabaseName");
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
            var imageInfoCollectionDefinition = new DocumentCollection
            {
                Id = imageInfoCollectionName,
            };

            imageInfoCollectionDefinition.PartitionKey.Paths.Add("/id");
            this.imageCollections = await this.documentClient.CreateDocumentCollectionIfNotExistsAsync(this.database.SelfLink, imageInfoCollectionDefinition, useSharedOffer ? null : requestOptions);
        }

        private async Task EnsureInitializedAsync()
        {
            await this.initializeTask.Value;
        }
    }
}