// <copyright file="IAppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
    using System.Collections.Generic;

    /// <summary>
    /// Used to store all the app settings
    /// </summary>
    public interface IAppSettings
    {
        /// <summary>
        /// Gets a value indicating whether testing mode is enabled.
        /// </summary>
        bool IsTesting { get; }

        /// <summary>
        /// Gets CertName
        /// </summary>
        string BotCertName { get; }

        /// <summary>
        /// Gets MicrosoftAppId
        /// </summary>
        string MicrosoftAppId { get; }

        /// <summary>
        /// Gets Testing
        /// </summary>
        string Testing { get; }

        /// <summary>
        /// Gets cosmos Endpoint
        /// </summary>
        string CosmosDBEndpointUrl { get; }

        /// <summary>
        /// Gets CosmosDBDatabaseName
        /// </summary>
        string CosmosDBDatabaseName { get; }

        /// <summary>
        /// Gets CosmosCollectionTeams
        /// </summary>
        string CosmosCollectionTeams { get; }

        /// <summary>
        /// Gets CosmosCollectionUsers
        /// </summary>
        string CosmosCollectionUsers { get; }

        /// <summary>
        /// Gets MaxPairUpsPerTeam
        /// </summary>
        int MaxPairUpsPerTeam { get; }

        /// <summary>
        /// Gets BotDisplayName
        /// </summary>
        string BotDisplayName { get; }

        /// <summary>
        /// Gets APPINSIGHTS_INSTRUMENTATIONKEY
        /// </summary>
        string AppInsightsInstrumentationKey { get; }

        /// <summary>
        /// Gets AppBaseDomain
        /// </summary>
        string AppBaseDomain { get; }

        /// <summary>
        /// Gets DefaultCulture
        /// </summary>
        string DefaultCulture { get; }

        /// <summary>
        /// Gets ManifestAppId
        /// </summary>
        string ManifestAppId { get; }

        /// <summary>
        /// Gets a value indicating whether tenant filter is disabled.
        /// </summary>
        bool DisableTenantFilter { get; }

        /// <summary>
        /// Gets AllowedTenant Ids.
        /// </summary>
        HashSet<string> AllowedTenantIds { get; }

        /// <summary>
        /// Gets CosmosDB KV key's name.
        /// </summary>
        string CosmosDBKeyName { get; }

        /// <summary>
        /// Gets LogicApp's KV key name.
        /// </summary>
        string ParingKeyName { get; }

        /// <summary>
        /// Gets App passowrd's KV key name.
        /// </summary>
        string MicrosoftAppPasswordKeyName { get; }
    }
}
