// <copyright file="IBotDataProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Icebreaker.Helpers;

    /// <summary>
    /// Data provider routines
    /// </summary>
    public interface IBotDataProvider
    {
        /// <summary>
        /// Get the list of teams to which the app was installed.
        /// </summary>
        /// <returns>List of installed teams</returns>
        Task<IList<TeamInstallInfo>> GetInstalledTeamsAsync();

        /// <summary>
        /// Get the stored information about given users
        /// </summary>
        /// <returns>User information</returns>
        Task<Dictionary<string, bool>> GetAllUsersOptInStatusAsync();

        /// <summary>
        /// Returns the team that the bot has been installed to
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <returns>Team that the bot is installed to</returns>
        Task<TeamInstallInfo> GetInstalledTeamAsync(string teamId);

        /// <summary>
        /// Get the stored information about the given user
        /// </summary>
        /// <param name="userId">User id</param>
        /// <returns>User information</returns>
        Task<UserInfo> GetUserInfoAsync(string userId);

        /// <summary>
        /// Updates team installation status in store. If the bot is installed, the info is saved, otherwise info for the team is deleted.
        /// </summary>
        /// <param name="team">The team installation info</param>
        /// <param name="installed">Value that indicates if bot is installed</param>
        /// <returns>Tracking task</returns>
        Task UpdateTeamInstallStatusAsync(TeamInstallInfo team, bool installed);

        /// <summary>
        /// Set the user info for the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <returns>Tracking task</returns>
        Task SetUserInfoAsync(string tenantId, string userId, bool optedIn, string serviceUrl);

        /// <summary>
        /// Set the user info for the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <param name="recentPairUps">User recent pairs</param>
        /// <returns>Tracking task</returns>
        Task SetUserInfoAsync(string tenantId, string userId, bool optedIn, string serviceUrl, List<UserInfo> recentPairUps);
    }
}