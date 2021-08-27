// <copyright file="ISecretsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
    using System.Threading.Tasks;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Secrets provider intereface.
    /// </summary>
    public interface ISecretsProvider
    {
        /// <summary>
        /// Gets Cosmos key
        /// </summary>
        /// <returns>Cosmos Db key.</returns>
        string GetCosmosDBKey();

        /// <summary>
        /// Gets LogicAppKey
        /// </summary>
        /// <returns>Logic app key.</returns>
        string GetLogicAppKey();

        /// <summary>
        /// Gets AppCredentials
        /// </summary>
        /// <returns>App Credentials.</returns>
        Task<AppCredentials> GetAppCredentialsAsync();
    }
}
