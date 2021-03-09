// <copyright file="SecretsHelper.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using Icebreaker.Interfaces;
    using Microsoft.Azure;

    /// <summary>
    /// Used to fetch secrets from reliable sources
    /// </summary>
    public class SecretsHelper : ISecretsHelper
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsHelper"/> class.
        /// </summary>
        public SecretsHelper()
        {
            this.SetSecretsValues();
        }

        /// <summary>
        /// Gets app client secret
        /// </summary>
        public string MicrosoftAppPassword { get; private set; }

        /// <summary>
        /// Gets Key used in logic app
        /// </summary>
        public string Key { get; private set; }

        /// <summary>
        /// Gets Cosmos DB master key
        /// </summary>
        public string CosmosDBKey { get; private set; }

        private void SetSecretsValues()
        {
            string microsoftAppPassword,
                key,
                cosmosDBKey,
                keyVaultUrl = Environment.GetEnvironmentVariable("KeyVaultURI");

            // if KeyVault uri is defined use it, otherwise use web configuration
            if (!string.IsNullOrEmpty(keyVaultUrl))
            {
                var keyVaultClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());

                microsoftAppPassword = this.GetKeyVaultSecret(keyVaultClient, nameof(this.MicrosoftAppPassword));
                key = this.GetKeyVaultSecret(keyVaultClient, nameof(this.Key));
                cosmosDBKey = this.GetKeyVaultSecret(keyVaultClient, nameof(this.CosmosDBKey));
            }
            else
            {
                microsoftAppPassword = CloudConfigurationManager.GetSetting(nameof(this.MicrosoftAppPassword));
                key = CloudConfigurationManager.GetSetting(nameof(this.Key));
                cosmosDBKey = CloudConfigurationManager.GetSetting(nameof(this.CosmosDBKey));
            }

            this.Key = key;
            this.CosmosDBKey = cosmosDBKey;
            this.MicrosoftAppPassword = microsoftAppPassword;
        }

        private string GetKeyVaultSecret(SecretClient client, string key)
        {
            return client.GetSecret(key).Value.Value;
        }
    }
}