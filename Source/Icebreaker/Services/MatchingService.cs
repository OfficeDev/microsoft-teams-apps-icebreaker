// <copyright file="MatchingService.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Services
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Helpers;
    using Icebreaker.Helpers.AdaptiveCards;
    using Icebreaker.Interfaces;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public class MatchingService : IMatchingService
    {
        private readonly IBotDataProvider dataProvider;
        private readonly ConversationHelper conversationHelper;
        private readonly TelemetryClient telemetryClient;
        private readonly BotAdapter botAdapter;
        private readonly int maxPairUpsPerTeam;
        private readonly int maxRecentPairUpsToPersistPerUser;
        private readonly string botDisplayName;

        /// <summary>
        /// Initializes a new instance of the <see cref="MatchingService"/> class.
        /// </summary>
        /// <param name="dataProvider">The data provider to use</param>
        /// <param name="conversationHelper">Conversation helper instance to notify team members</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        /// <param name="botAdapter">Bot adapter.</param>
        public MatchingService(IBotDataProvider dataProvider, ConversationHelper conversationHelper, TelemetryClient telemetryClient, BotAdapter botAdapter)
        {
            this.dataProvider = dataProvider;
            this.conversationHelper = conversationHelper;
            this.telemetryClient = telemetryClient;
            this.botAdapter = botAdapter;
            this.maxPairUpsPerTeam = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"));
            this.maxRecentPairUpsToPersistPerUser = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxRecentPairUpsToPersistPerUser"));
            this.botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");
        }

        /// <summary>
        /// Generate pairups and send pairup notifications.
        /// </summary>
        /// <returns>The number of pairups that were made</returns>
        public async Task<int> MakePairsAndNotifyAsync()
        {
            this.telemetryClient.TrackTrace("Making pairups");

            // Recall all the teams where we have been added
            // For each team where bot has been added:
            //     Pull the roster of the team
            //     Remove the members who have opted out of pairups
            //     Match each member with someone else
            //     Save this pair
            // Now notify each pair found in 1:1 and ask them to reach out to the other person
            // When contacting the user in 1:1, give them the button to opt-out
            var installedTeamsCount = 0;
            var pairsNotifiedCount = 0;
            var usersNotifiedCount = 0;
            var dbMembersCount = 0;

            try
            {
                var teams = await this.dataProvider.GetInstalledTeamsAsync();
                installedTeamsCount = teams.Count;
                this.telemetryClient.TrackTrace($"Generating pairs for {installedTeamsCount} teams");

                // Fetch all db users opt-in status/lookup
                var dbMembersLookup = await this.dataProvider.GetAllUsersOptInStatusAsync();
                dbMembersCount = dbMembersLookup.Count;

                foreach (var team in teams)
                {
                    this.telemetryClient.TrackTrace($"Pairing members of team {team.Id}");

                    try
                    {
                        var teamName = await this.conversationHelper.GetTeamNameByIdAsync(this.botAdapter, team);
                        var optedInUsers = await this.GetOptedInUsersAsync(dbMembersLookup, team);

                        foreach (var pair in this.MakePairs(optedInUsers, team).Take(this.maxPairUpsPerTeam))
                        {
                            usersNotifiedCount += await this.NotifyPairAsync(team, teamName, pair, default(CancellationToken));
                            pairsNotifiedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.telemetryClient.TrackTrace($"Error pairing up team members: {ex.Message}", SeverityLevel.Warning);
                        this.telemetryClient.TrackException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error making pairups: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }

            // Log telemetry about the pairups
            var properties = new Dictionary<string, string>
            {
                { "InstalledTeamsCount", installedTeamsCount.ToString() },
                { "PairsNotifiedCount", pairsNotifiedCount.ToString() },
                { "UsersNotifiedCount", usersNotifiedCount.ToString() },
                { "DBMembersCount", dbMembersCount.ToString() },
            };
            this.telemetryClient.TrackEvent("ProcessedPairups", properties);

            this.telemetryClient.TrackTrace($"Made {pairsNotifiedCount} pairups, {usersNotifiedCount} notifications sent");
            return pairsNotifiedCount;
        }

        /// <summary>
        /// Notify a pairup.
        /// </summary>
        /// <param name="teamModel">DB team model info.</param>
        /// <param name="teamName">MS-Teams team name</param>
        /// <param name="pair">The pairup</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Number of users notified successfully</returns>
        private async Task<int> NotifyPairAsync(TeamInstallInfo teamModel, string teamName, Tuple<ChannelAccount, ChannelAccount> pair, CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"Sending pairup notification to {pair?.Item1?.Id} and {pair?.Item2?.Id}");

            var teamsPerson1 = JObject.FromObject(pair.Item1).ToObject<TeamsChannelAccount>();
            var teamsPerson2 = JObject.FromObject(pair.Item2).ToObject<TeamsChannelAccount>();

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson1, teamsPerson2, this.botDisplayName);

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson2, teamsPerson1, this.botDisplayName);

            // Send notifications and return the number that was successful
            var notifyResults = await Task.WhenAll(
                this.conversationHelper.NotifyUserAsync(this.botAdapter, teamModel.ServiceUrl, teamModel.TeamId, MessageFactory.Attachment(cardForPerson1), teamsPerson1, teamModel.TenantId, cancellationToken),
                this.conversationHelper.NotifyUserAsync(this.botAdapter, teamModel.ServiceUrl, teamModel.TeamId, MessageFactory.Attachment(cardForPerson2), teamsPerson2, teamModel.TenantId, cancellationToken));
            return notifyResults.Count(wasNotified => wasNotified);
        }

        /// <summary>
        /// Get list of opted in users to start matching process
        /// </summary>
        /// <param name="dbMembersLookup">Lookup of DB users opt-in status</param>
        /// <param name="teamInfo">The team that the bot has been installed to</param>
        /// <returns>Opted in users' channels</returns>
        private async Task<List<ChannelAccount>> GetOptedInUsersAsync(Dictionary<string, bool> dbMembersLookup, TeamInstallInfo teamInfo)
        {
            // Pull the roster of specified team and then remove everyone who has opted out explicitly
            var members = await this.conversationHelper.GetTeamMembers(this.botAdapter, teamInfo);

            this.telemetryClient.TrackTrace($"Found {members.Count} in team {teamInfo.TeamId}");

            return members
                .Where(member => member != null)
                .Where(member =>
                {
                    var memberObjectId = this.GetChannelUserObjectId(member);
                    return !dbMembersLookup.ContainsKey(memberObjectId) || dbMembersLookup[memberObjectId];
                })
                .ToList();
        }

        /// <summary>
        /// Extract user Aad object id from channel account
        /// </summary>
        /// <param name="account">User channel account</param>
        /// <returns>Aad object id Guid value</returns>
        private string GetChannelUserObjectId(ChannelAccount account)
        {
            return JObject.FromObject(account).ToObject<TeamsChannelAccount>()?.AadObjectId;
        }

        /// <summary>
        /// Pair list of users into groups of 2 users per group
        /// </summary>
        /// <param name="users">Users accounts</param>
        /// <param name="teamModel">DB team model info.</param>
        /// <returns>List of pairs</returns>
        private List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users, TeamInstallInfo teamModel)
        {
            if (users.Count > 1)
            {
                this.telemetryClient.TrackTrace($"Making {users.Count / 2} pairs among {users.Count} users");
            }
            else
            {
                this.telemetryClient.TrackTrace($"Pairs could not be made because there is only 1 user in the team");
            }

            this.Randomize(users);
            LinkedList<ChannelAccount> queue = new LinkedList<ChannelAccount>(users);
            var pairs = new List<Tuple<ChannelAccount, ChannelAccount>>();

            while (queue.Count > 0)
            {
                ChannelAccount pairUserOne = queue.First.Value;
                ChannelAccount pairUserTwo = null;

                UserInfo pairUserOneInfo = this.GetOrCreateUserInfoAsync(this.GetChannelUserObjectId(pairUserOne), teamModel)?.Result;
                this.telemetryClient.TrackTrace($"Dequeuing (1) {pairUserOneInfo?.UserId}");
                queue.RemoveFirst();

                if (pairUserOneInfo.RecentPairUps == null)
                    pairUserOneInfo.RecentPairUps = new List<UserInfo>();

                bool foundPerfectPairing = false;

                for (LinkedListNode<ChannelAccount> restOfQueue = queue.First; restOfQueue != null; restOfQueue = restOfQueue.Next)
                {
                    pairUserTwo = restOfQueue.Value;
                    UserInfo pairUserTwoInfo = this.GetOrCreateUserInfoAsync(this.GetChannelUserObjectId(pairUserTwo), teamModel)?.Result;

                    if (pairUserTwoInfo.RecentPairUps == null)
                        pairUserTwoInfo.RecentPairUps = new List<UserInfo>();

                    this.telemetryClient.TrackTrace($"Processing {pairUserOneInfo?.UserId} and {pairUserTwoInfo?.UserId}");

                    // check if userone and usertwo have already paired recently
                    if (this.SamePairNotCreatedRecently(pairUserOneInfo, pairUserTwoInfo))
                    {
                        this.telemetryClient.TrackTrace($"Pairing {pairUserOneInfo?.UserId} and {pairUserTwoInfo?.UserId}");

                        pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(pairUserOne, pairUserTwo));
                        this.UpdateUserRecentlyPairedAsync(pairUserOneInfo, pairUserTwoInfo);
                        this.UpdateUserRecentlyPairedAsync(pairUserTwoInfo, pairUserOneInfo);

                        // Remove pairUserTwo since user has been paired
                        this.telemetryClient.TrackTrace($"Dequeuing (2) {pairUserTwoInfo?.UserId}");
                        queue.Remove(pairUserTwo);
                        foundPerfectPairing = true;
                        break;
                    }
                }

                // Not possible to find a perfect pairing, so just use next.
                if (!foundPerfectPairing)
                {
                    this.telemetryClient.TrackTrace($"No perfect pair; selecting next user");
                    pairUserTwo = queue.First?.Value;

                    if (pairUserTwo != null)
                    {
                        pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(pairUserOne, pairUserTwo));
                        queue.RemoveFirst();
                        this.telemetryClient.TrackTrace($"Pair formed; dequeued next user");
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"No more users left to pair with");
                    }
                }
            }

            this.telemetryClient.TrackTrace($"Formed {pairs.Count} pairs");

            return pairs;
        }

        /// <summary>
        /// Gets user info from the data store, or else generates it
        /// </summary>
        /// <param name="userId">User object Id</param>
        /// <param name="teamModel">DB team model info</param>
        /// <returns>List of pairs</returns>
        private async Task<UserInfo> GetOrCreateUserInfoAsync(string userId, TeamInstallInfo teamModel)
        {
            this.telemetryClient.TrackTrace($"Getting info for {userId}");

            UserInfo userInfo = await this.dataProvider.GetUserInfoAsync(userId);

            if (userInfo == null)
            {
                this.telemetryClient.TrackTrace($"{userId} info is not saved, generating now");

                userInfo = new UserInfo()
                {
                    TenantId = teamModel.TenantId,
                    UserId = userId,
                    OptedIn = true,
                    ServiceUrl = teamModel.ServiceUrl,
                    RecentPairUps = new List<UserInfo>(),
                };
            }

            return userInfo;
        }

        /// <summary>
        /// This method serves to update the pair's respective "RecentlyPaired" fields with each other.
        /// </summary>
        /// <param name="userOneInfo">UserInfo of the first user in pair</param>
        /// <param name="userTwoInfo">UserInfo of the second user in pair</param>
        private async void UpdateUserRecentlyPairedAsync(UserInfo userOneInfo, UserInfo userTwoInfo)
        {
            if (userOneInfo.RecentPairUps.Count == this.maxRecentPairUpsToPersistPerUser)
            {
                userOneInfo.RecentPairUps.RemoveAt(0);
            }

            userOneInfo.RecentPairUps.Add(userTwoInfo);

            this.telemetryClient.TrackTrace($"Updating user info for {userOneInfo?.UserId}");

            await this.SetUserInfoAsync(userOneInfo);

            this.telemetryClient.TrackTrace($"Successfully updated user info for {userOneInfo?.UserId}");
        }

        /// <summary>
        /// Set the user info for the given user (from UserInfo object)
        /// </summary>
        /// <param name="userInfo">User info</param>
        /// <returns>Tracking task</returns>
        private async Task SetUserInfoAsync(UserInfo userInfo)
        {
            await this.SetUserInfoAsync(userInfo.TenantId, userInfo.UserId, userInfo.OptedIn, userInfo.ServiceUrl, userInfo.RecentPairUps);
        }

        /// <summary>
        /// Set the user info for the given user
        /// </summary>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="optedIn">User opt-in status</param>
        /// <param name="serviceUrl">User service URL</param>
        /// <param name="recentPairUps">User recent pairs</param>
        /// <returns>Tracking task</returns>
        private async Task SetUserInfoAsync(string tenantId, string userId, bool optedIn, string serviceUrl, List<UserInfo> recentPairUps)
        {
            try
            {
                await this.dataProvider.SetUserInfoAsync(
                    tenantId,
                    userId,
                    optedIn,
                    serviceUrl,
                    recentPairUps
                        .Where(u => u.UserId != userId)
                        .Select(u => new UserInfo()
                        {
                            TenantId = u.TenantId,
                            UserId = u.UserId,
                            OptedIn = u.OptedIn,
                            ServiceUrl = u.ServiceUrl,
                            RecentPairUps = null,
                        })
                        .ToList());
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error updating user info: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// This method returns True if UserOne in pair was not 'recently matched' with UserTwo
        /// </summary>
        /// <param name="userOneInfo">UserInfo of the first user in pair</param>
        /// <param name="userTwoInfo">UserInfo of the second user in pair</param>
        /// <returns>True if users were NOT paired recently</returns>
        private bool SamePairNotCreatedRecently(UserInfo userOneInfo, UserInfo userTwoInfo)
        {
            if (userOneInfo == null || userTwoInfo == null)
            {
                return false;
            }

            this.telemetryClient.TrackTrace($"Check if {userOneInfo.UserId} and {userTwoInfo.UserId} have been recently paired");

            return !userOneInfo.RecentPairUps.Any(u => u.UserId == userTwoInfo.UserId) &&
                !userTwoInfo.RecentPairUps.Any(u => u.UserId == userOneInfo.UserId);
        }

        /// <summary>
        /// Randomize list of users
        /// </summary>
        /// <typeparam name="T">Generic item type</typeparam>
        /// <param name="items">List of users to randomize</param>
        private void Randomize<T>(IList<T> items)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());

            // For each spot in the array, pick
            // a random item to swap into that spot.
            for (int i = 0; i < items.Count - 1; i++)
            {
                int j = rand.Next(i, items.Count);
                T temp = items[i];
                items[i] = items[j];
                items[j] = temp;
            }
        }
    }
}