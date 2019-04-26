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
    using Microsoft.Azure;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public class IcebreakerBot
    {
        private readonly IcebreakerBotDataProvider dataProvider;
        private readonly TelemetryClient telemetryClient;
        private readonly int maxPairUpsPerTeam;
        private readonly string botDisplayName;
        private readonly string botId;
        private readonly bool isTesting;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBot"/> class.
        /// </summary>
        /// <param name="dataProvider">The data provider to use</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public IcebreakerBot(IcebreakerBotDataProvider dataProvider, TelemetryClient telemetryClient)
        {
            this.dataProvider = dataProvider;
            this.telemetryClient = telemetryClient;
            this.maxPairUpsPerTeam = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"));
            this.botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");
            this.botId = CloudConfigurationManager.GetSetting("MicrosoftAppId");
            this.isTesting = Convert.ToBoolean(CloudConfigurationManager.GetSetting("Testing"));
        }

        /// <summary>
        /// Generate pairups and send pairup notifications.
        /// </summary>
        /// <returns>The number of pairups that were made</returns>
        public async Task<int> MakePairsAndNotify()
        {
            this.telemetryClient.TrackTrace("Hit the MakePairsAndNotify method");

            // Recall all the teams where we have been added
            // For each team where I have been added:
            //     Pull the roster of each team where I have been added
            //     Remove the members who have opted out of pairs
            //     Match each member with someone else
            //     Save this pair
            // Now notify each pair found in 1:1 and ask them to reach out to the other person
            // When contacting the user in 1:1, give them the button to opt-out.

            // Get teams to which the app has been installed
            var teams = await this.dataProvider.GetInstalledTeamsAsync();

            var countPairsNotified = 0;

            this.telemetryClient.TrackTrace($"Retrieved {teams.Count} teams");

            this.telemetryClient.TrackTrace($"{this.maxPairUpsPerTeam} pairs maximum");

            foreach (var team in teams)
            {
                try
                {
                    MicrosoftAppCredentials.TrustServiceUrl(team.ServiceUrl);
                    var connectorClient = new ConnectorClient(new Uri(team.ServiceUrl));

                    var optedInUsers = await this.GetOptedInUsers(connectorClient, team);
                    var teamName = await this.GetTeamNameAsync(connectorClient, team.TeamId);

                    this.telemetryClient.TrackTrace($"Trying to pair members of {teamName} at: " + DateTime.Now.ToString());

                    foreach (var pair in this.MakePairs(optedInUsers).Take(this.maxPairUpsPerTeam))
                    {
                        await this.NotifyPair(connectorClient, team.TenantId, teamName, pair);

                        countPairsNotified++;
                    }
                }
                catch (UnauthorizedAccessException uae)
                {
                    this.telemetryClient.TrackException(uae);
                }
            }

            this.telemetryClient.TrackTrace($"{countPairsNotified} pairs notified at: " + DateTime.Now.ToString());

            return countPairsNotified;
        }

        /// <summary>
        /// Method that will return the information of the installed team
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The team that the bot has been installed to</returns>
        public Task<TeamInstallInfo> GetInstalledTeam(string tenantId, string teamId)
        {
            return this.dataProvider.GetTeamInstallInfoAsync(tenantId, teamId);
        }

        /// <summary>
        /// Send a welcome message to the user that was just added to a team.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="memberAddedId">The id of the added user</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The id of the team the user was added to</param>
        /// <param name="botInstaller">The person that installed the bot</param>
        /// <returns>Tracking task</returns>
        public async Task WelcomeUser(ConnectorClient connectorClient, string memberAddedId, string tenantId, string teamId, string botInstaller)
        {
            var teamName = await this.GetTeamNameAsync(connectorClient, teamId);
            var allMembers = await connectorClient.Conversations.GetConversationMembersAsync(teamId);

            ChannelAccount userThatJustJoined = null;
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
                this.telemetryClient.TrackTrace($"A new user just joined - {userThatJustJoined.AsTeamsChannelAccount().ObjectId}, {userThatJustJoined.AsTeamsChannelAccount().GivenName}");
                var welcomeMessageCard = WelcomeNewMemberCard.GetCard(teamName, userThatJustJoined.Name, this.botDisplayName, botInstaller);
                await this.NotifyUser(connectorClient, welcomeMessageCard, userThatJustJoined, tenantId);
            }
        }

        /// <summary>
        /// Sends a welcome message to the General channel of the team that this bot has been installed to
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The id of the team that the bot is installed to</param>
        /// <param name="botInstaller">The installer of the application</param>
        /// <returns>Tracking task</returns>
        public async Task WelcomeTeam(ConnectorClient connectorClient, string tenantId, string teamId, string botInstaller)
        {
            this.telemetryClient.TrackTrace("Hit the WelcomeTeam method with teamId = " + teamId);

            var teamName = await this.GetTeamNameAsync(connectorClient, teamId);

            var welcomeTeamMessageCard = WelcomeTeamAdaptiveCard.GetCard(teamName, this.botDisplayName, botInstaller);

            await this.NotifyTeam(connectorClient, welcomeTeamMessageCard, teamId);
        }

        /// <summary>
        /// Save information about the team to which the bot was added.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="botInstaller">Person that has added the bot to the team</param>
        /// <returns>Tracking task</returns>
        public Task SaveAddedToTeam(string serviceUrl, string teamId, string tenantId, string botInstaller)
        {
            return this.dataProvider.UpdateTeamInstallStatusAsync(new TeamInstallInfo { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId, InstallerName = botInstaller }, true);
        }

        /// <summary>
        /// Save information about the team from which the bot was removed.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <returns>Tracking task</returns>
        public Task SaveRemoveFromTeam(string serviceUrl, string teamId, string tenantId)
        {
            return this.dataProvider.UpdateTeamInstallStatusAsync(new TeamInstallInfo { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, false);
        }

        /// <summary>
        /// Opt out the user from further pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userId">The user id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public Task OptOutUser(string tenantId, string userId, string serviceUrl)
        {
            return this.dataProvider.SetUserInfoAsync(tenantId, userId, false, serviceUrl);
        }

        /// <summary>
        /// Opt in the user to pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userId">The user id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public Task OptInUser(string tenantId, string userId, string serviceUrl)
        {
            return this.dataProvider.SetUserInfoAsync(tenantId, userId, true, serviceUrl);
        }

        /// <summary>
        /// Get the name of a team.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The name of the team</returns>
        private async Task<string> GetTeamNameAsync(ConnectorClient connectorClient, string teamId)
        {
            this.telemetryClient.TrackTrace("Getting the team name now");

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
        private async Task NotifyPair(ConnectorClient connectorClient, string tenantId, string teamName, Tuple<ChannelAccount, ChannelAccount> pair)
        {
            this.telemetryClient.TrackTrace("Hit the NotifyPair method");

            var teamsPerson1 = pair.Item1.AsTeamsChannelAccount();
            var teamsPerson2 = pair.Item2.AsTeamsChannelAccount();

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson1.Name, teamsPerson2.Name, teamsPerson1.GivenName, teamsPerson2.GivenName, teamsPerson2.GivenName, teamsPerson1.UserPrincipalName, this.botDisplayName);

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson2.Name, teamsPerson1.Name, teamsPerson2.GivenName, teamsPerson1.GivenName, teamsPerson1.GivenName, teamsPerson2.UserPrincipalName, this.botDisplayName);

            this.telemetryClient.TrackTrace($"Notifying user - {teamsPerson1.ObjectId}, {teamsPerson1.GivenName}");
            await this.NotifyUser(connectorClient, cardForPerson1, teamsPerson1, tenantId);

            this.telemetryClient.TrackTrace($"Notifying user - {teamsPerson2.ObjectId}, {teamsPerson2.GivenName}");
            await this.NotifyUser(connectorClient, cardForPerson2, teamsPerson2, tenantId);
        }

        private async Task NotifyUser(ConnectorClient connectorClient, string cardToSend, ChannelAccount user, string tenantId)
        {
            this.telemetryClient.TrackTrace("Hit the NotifyUser method");

            // ensure conversation exists
            var me = new ChannelAccount { Id = this.botId };
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

            if (!this.isTesting)
            {
                // shoot the activity over
                await connectorClient.Conversations.SendToConversationAsync(activity);
            }
        }

        /// <summary>
        /// Method that will send out the message in the General channel of the team
        /// that this bot has been installed to
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="cardToSend">The actual welcome card (for the team)</param>
        /// <param name="teamId">The team id</param>
        /// <returns>A tracking task</returns>
        private async Task NotifyTeam(ConnectorClient connectorClient, string cardToSend, string teamId)
        {
            this.telemetryClient.TrackTrace("Hit the NotifyTeam method");

            var activity = new Activity()
            {
                Type = ActivityTypes.Message,
                Conversation = new ConversationAccount()
                {
                    Id = teamId
                },
                Attachments = new List<Attachment>()
                {
                    new Attachment()
                    {
                        ContentType = "application/vnd.microsoft.card.adaptive",
                        Content = JsonConvert.DeserializeObject(cardToSend)
                    }
                }
            };

            await connectorClient.Conversations.SendToConversationAsync(activity);
        }

        private async Task<List<ChannelAccount>> GetOptedInUsers(ConnectorClient connectorClient, TeamInstallInfo teamInfo)
        {
            this.telemetryClient.TrackTrace("Hit the GetOptedInUsers method");
            var optedInUsers = new List<ChannelAccount>();

            // Pull the roster of specified team and then remove everyone who has opted out explicitly
            var members = await connectorClient.Conversations.GetConversationMembersAsync(teamInfo.TeamId);
            if (members.Count > 1)
            {
                this.telemetryClient.TrackTrace($"There are {members.Count} members found in {teamInfo.TeamId}");
            }
            else
            {
                this.telemetryClient.TrackTrace("There are not enough members found");
            }

            foreach (var member in members)
            {
                var userInfo = await this.dataProvider.GetUserInfoAsync(teamInfo.TenantId, member.AsTeamsChannelAccount().ObjectId);
                if (userInfo == null || userInfo.OptedIn)
                {
                    this.telemetryClient.TrackTrace($"Adding {member.Name} to the list at");
                    optedInUsers.Add(member);
                }
            }

            return optedInUsers;
        }

        private List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users)
        {
            this.telemetryClient.TrackTrace("Hit the MakePairs method");

            if (users.Count > 1)
            {
                this.telemetryClient.TrackTrace($"There could be {users.Count / 2} pairs that could be made");
            }
            else
            {
                this.telemetryClient.TrackTrace($"Pairs could not be made because of having - {users.Count}");
            }

            var pairs = new List<Tuple<ChannelAccount, ChannelAccount>>();

            this.Randomize(users);

            for (int i = 0; i < users.Count - 1; i += 2)
            {
                pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(users[i], users[i + 1]));
            }

            return pairs;
        }

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