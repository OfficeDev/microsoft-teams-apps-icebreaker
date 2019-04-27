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
    using Newtonsoft.Json;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public class IcebreakerBot
    {
        private static TelemetryClient telemetry = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));
        private readonly IcebreakerBotDataProvider dataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBot"/> class.
        /// </summary>
        /// <param name="dataProvider">The data provider to use</param>
        public IcebreakerBot(IcebreakerBotDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        /// <summary>
        /// Generate pairups and send pairup notifications.
        /// </summary>
        /// <returns>The number of pairups that were made</returns>
        public async Task<int> MakePairsAndNotify()
        {
            telemetry.TrackTrace("Hit the MakePairsAndNotify method");

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
            var maxPairUpsPerTeam = Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"));

            telemetry.TrackTrace($"Retrieved {teams.Count} teams");

            telemetry.TrackTrace($"{maxPairUpsPerTeam} pairs maximum");

            foreach (var team in teams)
            {
                try
                {
                    MicrosoftAppCredentials.TrustServiceUrl(team.ServiceUrl);
                    var connectorClient = new ConnectorClient(new Uri(team.ServiceUrl));

                    var optedInUsers = await this.GetOptedInUsers(connectorClient, team);
                    var teamName = await this.GetTeamNameAsync(connectorClient, team.TeamId);

                    telemetry.TrackTrace($"Trying to pair members of {teamName} at: " + DateTime.Now.ToString());

                    foreach (var pair in this.MakePairs(optedInUsers).Take(maxPairUpsPerTeam))
                    {
                        await this.NotifyPair(connectorClient, team.TenantId, teamName, pair);

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
        /// Method that will return the information of the installed team
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The team that the bot has been installed to</returns>
        public Task<TeamInstallInfo> GetInstalledTeam(string tenantId, string teamId)
        {
            return this.dataProvider.GetInstalledTeamAsync(tenantId, teamId);
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

            var allMembers = await this.GetTeamMembers(connectorClient, teamId, tenantId);

            var botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");

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
                telemetry.TrackTrace($"A new user just joined - {userThatJustJoined.AsTeamsChannelAccount().ObjectId}, {userThatJustJoined.AsTeamsChannelAccount().GivenName}");
                var welcomeMessageCard = WelcomeNewMemberCard.GetCard(teamName, userThatJustJoined.Name, botDisplayName, botInstaller);
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
            telemetry.TrackTrace("Hit the WelcomeTeam method with teamId = " + teamId);

            var teamName = await this.GetTeamNameAsync(connectorClient, teamId);

            var botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");

            var welcomeTeamMessageCard = WelcomeTeamAdaptiveCard.GetCard(teamName, botDisplayName, botInstaller);

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
            return this.dataProvider.UpdateTeamInstallStatusAsync(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId, InstallerName = botInstaller }, true);
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
            return this.dataProvider.UpdateTeamInstallStatusAsync(new TeamInstallInfo() { ServiceUrl = serviceUrl, TeamId = teamId, TenantId = tenantId }, false);
        }

        /// <summary>
        /// Opt out the user from further pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userAadObjectId">The user AAD object id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public Task OptOutUser(string tenantId, string userAadObjectId, string serviceUrl)
        {
            return this.dataProvider.SetUserInfoAsync(tenantId, userAadObjectId, false, serviceUrl);
        }

        /// <summary>
        /// Opt in the user to pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userAadObjectId">The user AAD object id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        public Task OptInUser(string tenantId, string userAadObjectId, string serviceUrl)
        {
            return this.dataProvider.SetUserInfoAsync(tenantId, userAadObjectId, true, serviceUrl);
        }

        /// <summary>
        /// Get the name of a team.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="teamId">The team id</param>
        /// <returns>The name of the team</returns>
        private async Task<string> GetTeamNameAsync(ConnectorClient connectorClient, string teamId)
        {
            telemetry.TrackTrace("Getting the team name now");

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
            telemetry.TrackTrace("Hit the NotifyPair method");
            var displayName = CloudConfigurationManager.GetSetting("BotDisplayName");

            var teamsPerson1 = pair.Item1.AsTeamsChannelAccount();
            var teamsPerson2 = pair.Item2.AsTeamsChannelAccount();

            // Fill in person1's info in the card for person2
            var cardForPerson2 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson1.Name, teamsPerson2.Name, teamsPerson1.GivenName, teamsPerson2.GivenName, teamsPerson1.UserPrincipalName, displayName);

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationAdaptiveCard.GetCard(teamName, teamsPerson2.Name, teamsPerson1.Name, teamsPerson2.GivenName, teamsPerson1.GivenName, teamsPerson2.UserPrincipalName, displayName);

            telemetry.TrackTrace($"Notifying user - {teamsPerson1.ObjectId}, {teamsPerson1.GivenName}");
            await this.NotifyUser(connectorClient, cardForPerson1, teamsPerson1, tenantId);

            telemetry.TrackTrace($"Notifying user - {teamsPerson2.ObjectId}, {teamsPerson2.GivenName}");
            await this.NotifyUser(connectorClient, cardForPerson2, teamsPerson2, tenantId);
        }

        private async Task NotifyUser(ConnectorClient connectorClient, string cardToSend, ChannelAccount user, string tenantId)
        {
            telemetry.TrackTrace("Hit the NotifyUser method");

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
            telemetry.TrackTrace("Hit the NotifyTeam method");

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

        private async Task<List<ChannelAccount>> GetTeamMembers(ConnectorClient connectorClient, string teamId, string tenantId)
        {
            // Pull the roster of specified team and then remove everyone who has opted out explicitly
#pragma warning disable CS0618 // Type or member is obsolete
            var membersIList = await connectorClient.Conversations.GetConversationMembersAsync(teamId);
#pragma warning restore CS0618 // Type or member is obsolete
            var members = membersIList as List<ChannelAccount>;
            return members;
        }

        private async Task<List<ChannelAccount>> GetOptedInUsers(ConnectorClient connectorClient, TeamInstallInfo teamInfo)
        {
            telemetry.TrackTrace("Hit the GetOptedInUsers method");
            var optedInUsers = new List<ChannelAccount>();

            var members = await this.GetTeamMembers(connectorClient, teamInfo.TeamId, teamInfo.TenantId);

            if (members.Count > 1)
            {
                telemetry.TrackTrace($"There are {members.Count} members found in {teamInfo.TeamId}");
            }
            else
            {
                telemetry.TrackTrace("There are not enough members found");
            }

            foreach (var member in members)
            {
                var userInfo = await this.dataProvider.GetUserInfoAsync(teamInfo.TenantId, member.AsTeamsChannelAccount().ObjectId);
                if (userInfo == null || userInfo.OptedIn)
                {
                    telemetry.TrackTrace($"Adding {member.Name} to the list at");
                    optedInUsers.Add(member);
                }
            }

            return optedInUsers;
        }

        private List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users)
        {
            telemetry.TrackTrace("Hit the MakePairs method");

            if (users.Count > 1)
            {
                telemetry.TrackTrace($"There could be {users.Count / 2} pairs that could be made");
            }
            else
            {
                telemetry.TrackTrace($"Pairs could not be made because of having - {users.Count}");
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