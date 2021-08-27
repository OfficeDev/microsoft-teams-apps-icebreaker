// <copyright file="SecretOptions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    /// <summary>
    /// Secret options.
    /// </summary>
    public class SecretOptions
    {
        /// <summary>
        /// Gets or sets keyVaultUri
        /// </summary>
        public string KeyVaultUri { get; set; }

        /// <summary>
        /// Gets or sets app client secret
        /// </summary>
        public string MicrosoftAppPassword { get; set; }

        /// <summary>
        /// Gets or sets Key used in logic app
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets Cosmos DB master key
        /// </summary>
        public string CosmosDBKey { get; set; }
    }
}