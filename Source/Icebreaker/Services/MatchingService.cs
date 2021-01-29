//----------------------------------------------------------------------------------------------
// <copyright file="MatchingService.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Helpers;
    using Helpers.AdaptiveCards;
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

                var pairHistory = await this.dataProvider.GetPairHistoryAsync();
                var parsed = this.ParsePairHistory(pairHistory);
                var pastPairs = parsed.Item1;
                var prevIteration = parsed.Item2;
                prevIteration++;

                // dummyPair is recorded as a past pairing is to separate iteration cycles and avoid a deadlock
                var dummyPair = new Tuple<string, string>(null, null);
                await this.dataProvider.AddPairAsync(dummyPair, prevIteration);

                foreach (var team in teams)
                {
                    this.telemetryClient.TrackTrace($"Pairing members of team {team.Id}");
                    try
                    {
                        var teamName = await this.conversationHelper.GetTeamNameByIdAsync(this.botAdapter, team);
                        var optedInUsers = await this.GetOptedInUsersAsync(dbMembersLookup, team);

                        foreach (var pair in this.MakePairs(optedInUsers, pastPairs).Take(this.maxPairUpsPerTeam))
                        {
                            var pairId = new Tuple<string, string>(pair.Item1.Id, pair.Item2.Id);
                            await this.dataProvider.AddPairAsync(pairId, prevIteration);

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
            this.telemetryClient.TrackTrace($"Sending pairup notification to {pair.Item1.Id} and {pair.Item2.Id}");

            var teamsPerson1 = JObject.FromObject(pair.Item1).ToObject<TeamsChannelAccount>();
            var teamsPerson2 = JObject.FromObject(pair.Item2).ToObject<TeamsChannelAccount>();

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamModel.Id, teamName, teamsPerson1, teamsPerson2, this.botDisplayName);

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamModel.Id, teamName, teamsPerson2, teamsPerson1, this.botDisplayName);

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
        private async Task<List<ChannelAccount>> GetOptedInUsersAsync(Dictionary<string, Dictionary<string, bool>> dbMembersLookup, TeamInstallInfo teamInfo)
        {
            // Pull the roster of specified team and then remove everyone who has opted out explicitly
            var members = await this.conversationHelper.GetTeamMembers(this.botAdapter, teamInfo);

            this.telemetryClient.TrackTrace($"Found {members.Count} in team {teamInfo.Id}");

            return members
                .Where(member => member != null)
                .Where(member =>
                {
                    var memberObjectId = this.GetChannelUserObjectId(member);
                    return !dbMembersLookup.ContainsKey(memberObjectId) || dbMembersLookup[memberObjectId][teamInfo.Id];
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
        /// Parse through a list of pairing information.
        /// </summary>
        /// <param name="pairHistory">List of pairing information.</param>
        /// <returns>A tuple with "a dictionary mapping users' IDs to a set of other user IDs that
        /// the respective user has paired with in the previous iteration" and "the previous iteration ID"</returns>
        private Tuple<Dictionary<string, HashSet<string>>, int> ParsePairHistory(IList<PairInfo> pairHistory)
        {
            var pastPairs = new Dictionary<string, HashSet<string>>();

            // prevIteration defaults to 0 if pairHistory is empty.
            int prevIteration = pairHistory.Any() ? pairHistory.Select(pair => pair.Iteration).Max() : 0;

            foreach (var pair in pairHistory)
            {
                if (pair.Iteration < prevIteration || pair.User1Id == null)
                {
                    continue;
                }

                if (!pastPairs.ContainsKey(pair.User1Id))
                {
                    pastPairs[pair.User1Id] = new HashSet<string>();
                }

                if (!pastPairs.ContainsKey(pair.User2Id))
                {
                    pastPairs[pair.User2Id] = new HashSet<string>();
                }

                pastPairs[pair.User1Id].Add(pair.User2Id);
                pastPairs[pair.User2Id].Add(pair.User1Id);
            }

            return new Tuple<Dictionary<string, HashSet<string>>, int>(pastPairs, prevIteration);
        }

        /// <summary>
        /// Pair list of users into groups of 2 users per group
        /// </summary>
        /// <param name="users">Users accounts</param>
        /// <param name="pastPairs">Previously made pairings to avoid for future pairings.</param>
        /// <returns>List of pairs</returns>
        private List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users, Dictionary<string, HashSet<string>> pastPairs)
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

            var pairs = new List<Tuple<ChannelAccount, ChannelAccount>>();

            var currentPairs = new HashSet<string>();

            for (int a = 0; a < users.Count - 1; a++)
            {
                for (int b = a + 1; b < users.Count; b++)
                {
                    var user1 = users[a];
                    var user2 = users[b];

                    if (currentPairs.Contains(user1.Id) || currentPairs.Contains(user2.Id))
                    {
                        continue;
                    }

                    if (!pastPairs.ContainsKey(user1.Id) || !pastPairs[user1.Id].Contains(user2.Id))
                    {
                        // match them
                        pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(user1, user2));

                        if (!pastPairs.ContainsKey(user1.Id))
                        {
                            pastPairs[user1.Id] = new HashSet<string>();
                        }

                        if (!pastPairs.ContainsKey(user2.Id))
                        {
                            pastPairs[user2.Id] = new HashSet<string>();
                        }

                        pastPairs[user1.Id].Add(user2.Id);
                        pastPairs[user2.Id].Add(user1.Id);
                        currentPairs.Add(user1.Id);
                        currentPairs.Add(user2.Id);
                        break;
                    }
                }
            }

            return pairs;
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