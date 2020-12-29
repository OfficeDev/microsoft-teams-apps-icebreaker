// <copyright file="ISecretsHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Interfaces
{

    /// <summary>
    /// 
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