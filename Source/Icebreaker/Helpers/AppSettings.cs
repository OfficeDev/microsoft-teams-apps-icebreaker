// <copyright file="AppSettings.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    using System.Collections.Generic;
    using Icebreaker.Interfaces;

    /// <summary>
    /// AppSettings contains the static variables
    /// </summary>
    public class AppSettings : IAppSettings
    {
        /// <inheritdoc/>
        public string Testing { get; set; }

        /// <inheritdoc/>
        public string MicrosoftAppId { get; set; }

        /// <inheritdoc/>
        public string BotCertName { get; set; }

        /// <inheritdoc/>
        public string BotDisplayName { get; set; }

        /// <inheritdoc/>
        public string CosmosDBEndpointUrl { get; set; }

        /// <inheritdoc/>
        public string CosmosDBDatabaseName { get; set; }

        /// <inheritdoc/>
        public string CosmosCollectionTeams { get; set; }

        /// <inheritdoc/>
        public string CosmosCollectionUsers { get; set; }

        /// <inheritdoc/>
        public int MaxPairUpsPerTeam { get; set; }

        /// <inheritdoc/>
        public string AppInsightsInstrumentationKey { get; set; }

        /// <inheritdoc/>
        public bool DisableTenantFilter { get; set; }

        /// <inheritdoc/>
        public string AppBaseDomain { get; set; }

        /// <inheritdoc/>
        public string DefaultCulture { get; set; }

        /// <inheritdoc/>
        public HashSet<string> AllowedTenantIds { get; set; }

        /// <inheritdoc/>
        public bool IsTesting { get; set; }
    }
}