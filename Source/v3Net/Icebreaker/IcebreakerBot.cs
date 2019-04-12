//----------------------------------------------------------------------------------------------
// <copyright file="IcebreakerBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Helpers;
    using Helpers.AdaptiveCards;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Connector.Teams.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public static class IcebreakerBot
    {
        private static TelemetryClient telemetry = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));

        /// <summary>
        /// Generate pairups and send pairup notifications.
        /// </summary>
        /// <returns>The number of pairups that were made</returns>
        public static async Task<int> MakePairsAndNotify()
        {
            telemetry.TrackTrace("Hit the MakePairsAndNotify method at: " + DateTime.Now.ToString());

            // Recall all the teams where we have been added
            // For each team where I have been added:
            //     Pull the roster of each team where I have been added
            //     Remove the members who have opted out of pairs
            //     Match each member with someone else
            //     Save this pair
            // Now notify each pair found in 1:1 and ask them to reach out to the other person
            // When contacting the user in 1:1, give them the button to opt-out.

            // Get teams to which the app has been installed
            var teams = IcebreakerBotDataProvider.GetInstalledTeams();

            var countPairsNotified = 0;
            var maxPairUpsPerTeam = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"));

            telemetry.TrackTrace($"Retrieved {teams.Count} teams at: " + DateTime.Now.ToString());

            telemetry.TrackTrace($"{maxPairUpsPerTeam} pairs maximum");

            foreach (var team in teams)
            {
                try
                {
                    MicrosoftAppCredentials.TrustServiceUrl(team.ServiceUrl);
                    var connectorClient = new ConnectorClient(new Uri(team.ServiceUrl));

                    var optedInUsers = await GetOptedInUsers(connectorClient, team);
                    var teamName = await GetTeamNameAsync(connectorClient, team.TeamId);

                    telemetry.TrackTrace($"Trying to pair members of {teamName} at: " + DateTime.Now.ToString());

                    foreach (var pair in MakePairs(optedInUsers).Take(maxPairUpsPerTeam))
                    {
                        await NotifyPair(connectorClient, team.TenantId, teamName, pair);

                        countPairsNotified++;
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    telemetry.TrackException(uae);
                }
            }

            telemetry.TrackTrace($"{countPairsNotified} pairs notified at: " + DateTime.Now.ToString());

            return countPairsNotified;
        }

        /// <summary>
        /// Send a welcome message to the user that was just added to a team.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="memberAddedId">The id of the added user</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The id of the team the user was added to</param>
        /// <returns>Tracking task</returns>
        public static async Task WelcomeUser(ConnectorClient connectorClient, string memberAddedId, string tenantId, string teamId)
        {
            var teamName = await GetTeamNameAsync(connectorClient, teamId);

            var allMembers = await GetTeamMembers(connectorClient, teamId, tenantId);

            var botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");

            TeamsChannelAccount userThatJustJoined = null;

            foreach (var m in allMembers)
            {
                // both values are 29: values
                if (m.Id == memberAddedId)
                {
                    userThatJustJoined = m;
                }
            }

            if (userThatJustJoined != null)
            {
                telemetry.TrackTrace($"A new user just joined - {userThatJustJoined.ObjectId}, {userThatJustJoined.GivenName}");
                var welcomeMessageCard = WelcomeNewMemberCard.GetCard(teamName, userThatJustJoined.Name, botDisplayName);
                await NotifyUser(connectorClient, welcomeMessageCard, userThatJustJoined, tenantId);
            }
        }

        /// <summary>
        /// Save information about the team to which the bot was added.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <returns>Tracking task</returns>
        public static async Task SaveAddedToTeam(string serviceUrl, string teamId, string tenantId)
        {
            await IcebreakerBotDataProvider.SaveTeamInstallStatus(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, true);
        }

        /// <summary>
        /// Save information about the team from which the bot was removed.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <returns>Tracking task</returns>
        public static async Task SaveRemoveFromTeam(string serviceUrl, string teamId, string tenantId)
        {
            await IcebreakerBotDataProvider.SaveTeamInstallStatus(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, false);
        }

        /// <summary>
        /// Opt out the user from further pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userId">The user id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public static async Task OptOutUser(string tenantId, string userId, string serviceUrl)
        {
            await IcebreakerBotDataProvider.SetUserOptInStatus(tenantId, userId, false, serviceUrl);
        }

        /// <summary>
        /// Opt in the user to pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userId">The user id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public static async Task OptInUser(string tenantId, string userId, string serviceUrl)
        {
            await IcebreakerBotDataProvider.SetUserOptInStatus(tenantId, userId, true, serviceUrl);
        }

        /// <summary>
        /// Get the name of a team.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The name of the team</returns>
        private static async Task<string> GetTeamNameAsync(ConnectorClient connectorClient, string teamId)
        {
            telemetry.TrackTrace("Getting the team name now at: " + DateTime.Now.ToString());

            var teamsConnectorClient = connectorClient.GetTeamsConnectorClient();
            var teamDetailsResult = await teamsConnectorClient.Teams.FetchTeamDetailsAsync(teamId);
            return teamDetailsResult.Name;
        }

        /// <summary>
        /// Notify a pairup.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamName">The team name</param>
        /// <param name="pair">The pairup</param>
        /// <returns>Tracking task</returns>
        private static async Task NotifyPair(ConnectorClient connectorClient, string tenantId, string teamName, Tuple<ChannelAccount, ChannelAccount> pair)
        {
            telemetry.TrackTrace("Hit the NotifyPair method at: " + DateTime.Now.ToString());
            var displayName = CloudConfigurationManager.GetSetting("BotDisplayName");

            var teamsPerson1 = pair.Item1.AsTeamsChannelAccount();
            var teamsPerson2 = pair.Item2.AsTeamsChannelAccount();

            var firstPerson = teamsPerson1.Name;
            var secondPerson = teamsPerson2.Name;

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamName, firstPerson, secondPerson, teamsPerson2.GivenName, teamsPerson1.UserPrincipalName, displayName, teamsPerson1.Email);

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamName, secondPerson, firstPerson, teamsPerson1.GivenName, teamsPerson2.UserPrincipalName, displayName, teamsPerson2.Email);

            telemetry.TrackTrace($"Notifying user - {teamsPerson1.ObjectId}, {teamsPerson1.GivenName}");
            await NotifyUser(connectorClient, cardForPerson1, teamsPerson1, tenantId);

            telemetry.TrackTrace($"Notifying user - {teamsPerson2.ObjectId}, {teamsPerson2.GivenName}");
            await NotifyUser(connectorClient, cardForPerson2, teamsPerson2, tenantId);
        }

        private static async Task NotifyUser(ConnectorClient connectorClient, string cardToSend, ChannelAccount user, string tenantId)
        {
            telemetry.TrackTrace("Hit the NotifyUser method at: " + DateTime.Now.ToString());

            var me = new ChannelAccount()
            {
                Id = CloudConfigurationManager.GetSetting("MicrosoftAppId"),
                Name = CloudConfigurationManager.GetSetting("BotDisplayName")
            };

            // ensure conversation exists
            var response = connectorClient.Conversations.CreateOrGetDirectConversation(me, user, tenantId);

            // construct the activity we want to post
            var activity = new Activity()
            {
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount()
                {
                    Id = response.Id,
                },
                Attachments = new List<Attachment>()
                    {
                        new Attachment()
                        {
                            ContentType = "application/vnd.microsoft.card.adaptive",
                            Content = JsonConvert.DeserializeObject(cardToSend),
                        }
                    }
            };

            var isTesting = bool.Parse(CloudConfigurationManager.GetSetting("Testing"));

            if (!isTesting)
            {
                // shoot the activity over
                await connectorClient.Conversations.SendToConversationAsync(activity, response.Id);
            }
        }

        private static async Task<TeamsChannelAccount[]> GetTeamMembers(ConnectorClient connectorClient, string teamId, string tenantId)
        {
            // Pull the roster of specified team and then remove everyone who has opted out explicitly
#pragma warning disable CS0618 // Type or member is obsolete
            var members = await connectorClient.Conversations.GetTeamsConversationMembersAsync(teamId, tenantId);
#pragma warning restore CS0618 // Type or member is obsolete
            return members;
        }

        private static async Task<List<ChannelAccount>> GetOptedInUsers(ConnectorClient connectorClient, TeamInstallInfo teamInfo)
        {
            telemetry.TrackTrace("Hit the GetOptedInUsers method at: " + DateTime.Now.ToString());
            var optedInUsers = new List<ChannelAccount>();

            var members = await GetTeamMembers(connectorClient, teamInfo.TeamId, teamInfo.TenantId);

            if (members.Length > 1)
            {
                telemetry.TrackTrace($"There are {members.Length} members found in {teamInfo.TeamId} at: " + DateTime.Now.ToString());
            }
            else
            {
                telemetry.TrackTrace("There are not enough members found: " + DateTime.Now.ToString());
            }

            foreach (var member in members)
            {
                var optInStatus = IcebreakerBotDataProvider.GetUserOptInStatus(teamInfo.TenantId, member.ObjectId);

                if (optInStatus == null || optInStatus.OptedIn)
                {
                    telemetry.TrackTrace($"Adding {member.Name} to the list at: " + DateTime.Now.ToString());
                    optedInUsers.Add(member);
                }
            }

            return optedInUsers;
        }

        private static List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users)
        {
            telemetry.TrackTrace("Hit the MakePairs method at: " + DateTime.Now.ToString());

            if (users.Count > 1)
            {
                telemetry.TrackTrace($"There could be {users.Count / 2} pairs that could be made at: " + DateTime.Now.ToString());
            }
            else
            {
                telemetry.TrackTrace($"Pairs could not be made because of having - {users.Count} at: " + DateTime.Now.ToString());
            }

            var pairs = new List<Tuple<ChannelAccount, ChannelAccount>>();

            Randomize<ChannelAccount>(users);

            for (int i = 0; i < users.Count - 1; i += 2)
            {
                pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(users[i], users[i + 1]));
            }

            return pairs;
        }

        private static void Randomize<T>(IList<T> items)
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