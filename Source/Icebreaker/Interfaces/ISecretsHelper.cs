// <copyright file="ISecretsHelper.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
    /// <summary>
    /// Used to fetch secrets from reliable sources
    /// </summary>
    public interface ISecretsHelper
    {
        /// <summary>
        /// Gets app client secret
        /// </summary>
        string MicrosoftAppPassword { get; }

        /// <summary>
        /// Gets Key used in logic app
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Gets Cosmos DB master key
        /// </summary>
        string CosmosDBKey { get; }
    }
}