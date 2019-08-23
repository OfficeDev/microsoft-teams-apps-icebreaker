//----------------------------------------------------------------------------------------------
// <copyright file="IcebreakerBot.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Bots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Cards;
    using Icebreaker.Helpers;
    using Icebreaker.Properties;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public class IcebreakerBot : ActivityHandler
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IcebreakerBotDataProvider dataProvider;
        private readonly MicrosoftAppCredentials microsoftAppCredentials;
        private readonly string botId = CloudConfigurationManager.GetSetting("MicrosoftAppId");
        private readonly string botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBot"/> class.
        /// </summary>
        /// <param name="telemetryClient">The logging mechanism and logging.</param>
        /// <param name="dataProvider">The data provider.</param>
        /// <param name="microsoftAppCredentials">The Microsoft application credentials.</param>
        public IcebreakerBot(
            TelemetryClient telemetryClient,
            IcebreakerBotDataProvider dataProvider,
            MicrosoftAppCredentials microsoftAppCredentials)
        {
            this.telemetryClient = telemetryClient;
            this.dataProvider = dataProvider;
            this.microsoftAppCredentials = microsoftAppCredentials;
        }

        /// <summary>
        /// Generate pairups and send pairup notifications.
        /// </summary>
        /// <returns>The number of pairups that were made</returns>
        public async Task<int> MakePairsAndNotify()
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

            try
            {
                var teams = await this.dataProvider.GetInstalledTeamsAsync();
                installedTeamsCount = teams.Count;
                this.telemetryClient.TrackTrace($"Generating pairs for {installedTeamsCount} teams");
                var botId = "28:" + this.botId;

                foreach (var team in teams)
                {
                    this.telemetryClient.TrackTrace($"Pairing members of team {team.Id}");

                    try
                    {
                        MicrosoftAppCredentials.TrustServiceUrl(team.ServiceUrl);
                        var connectorClient = new ConnectorClient(new Uri(team.ServiceUrl), this.microsoftAppCredentials);

                        var optedInUsers = await this.GetOptedInUsers(connectorClient, team);

                        foreach (var pair in this.MakePairs(optedInUsers).Take(Convert.ToInt32(CloudConfigurationManager.GetSetting("MaxPairUpsPerTeam"))))
                        {
                            usersNotifiedCount += await this.NotifyPair(connectorClient, team.TenantId, team.TeamName, botId, pair);
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
            };
            this.telemetryClient.TrackEvent("ProcessedPairups", properties);

            this.telemetryClient.TrackTrace($"Made {pairsNotifiedCount} pairups, {usersNotifiedCount} notifications sent");
            return pairsNotifiedCount;
        }

        /// <inheritdoc/>
        public override Task OnTurnAsync(
            ITurnContext turnContext,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            switch (turnContext.Activity.Type)
            {
                case ActivityTypes.Message:
                    return this.OnMessageActivityAsync(new DelegatingTurnContext<IMessageActivity>(turnContext), cancellationToken);
                default:
                    return base.OnTurnAsync(turnContext, cancellationToken);
            }
        }

        /// <summary>
        /// The first method that is executed whenever the bot is being added to a team,
        /// or the first time that a user is interacting with the bot.
        /// </summary>
        /// <param name="turnContext">The current turn/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        protected override async Task OnConversationUpdateActivityAsync(
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            try
            {
                var activity = turnContext.Activity;

                this.telemetryClient.TrackTrace($"Received conversationUpdate activity");
                this.telemetryClient.TrackTrace($"conversationType: {activity.Conversation.ConversationType}, membersAdded: {activity.MembersAdded?.Count()}, membersRemoved: {activity.MembersRemoved?.Count()}");

                if (activity.MembersAdded?.Count() > 0)
                {
                    switch (activity.Conversation.ConversationType)
                    {
                        case "channel":
                            await this.OnMembersAddedToTeamAsync(activity.MembersAdded, turnContext, cancellationToken);
                            break;
                        default:
                            this.telemetryClient.TrackTrace($"Ignoring event from the conversation type: {activity.Conversation.ConversationType}");
                            break;
                    }
                }
                else if (activity.MembersRemoved?.Any(x => x.Id == activity.Recipient.Id) == true)
                {
                    this.telemetryClient.TrackTrace($"Bot removed from team {activity.Conversation.Id}");

                    var teamDetails = ((JObject)turnContext.Activity.ChannelData).ToObject<TeamsChannelData>();
                    var teamInstallInfo = new TeamInstallInfo
                    {
                        TeamId = teamDetails.Team.Id,
                        TenantId = teamDetails.Tenant.Id,
                        ServiceUrl = activity.ServiceUrl,
                        TeamName = teamDetails.Team.Name
                    };

                    await this.dataProvider.UpdateTeamInstallStatusAsync(teamInstallInfo, false);

                    var properties = new Dictionary<string, string>
                    {
                        { "Scope", activity.Conversation?.ConversationType },
                        { "TeamId", teamDetails.Team.Id },
                        { "UninstallerId", activity.From.Id },
                    };

                    this.telemetryClient.TrackEvent("AppUninstalled", properties);
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Ignoring conversationUpdate that was not a membersAdded event");
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error processing conversationUpdate: {ex.Message}");
            }
        }

        /// <summary>
        /// The method that fires whenever a message is coming to the bot from the user,
        /// or coming to the bot in the context of a team.
        /// </summary>
        /// <param name="turnContext">The current turn/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        protected override async Task OnMessageActivityAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            try
            {
                await this.SendTypingIndicatorAsync(turnContext);

                var message = turnContext.Activity;

                this.telemetryClient.TrackTrace($"Received message activity");
                this.telemetryClient.TrackTrace($"from: {message.From?.Id}conversation: {message.Conversation.Id}, replyToId: {message.ReplyToId}");

                switch (message.Conversation.ConversationType)
                {
                    case "personal":
                        await this.OnMessageActivityInPersonalChatAsync(message, turnContext, cancellationToken);
                        break;
                    case "channel":
                        await this.OnMessageActivityInChannelAsync(turnContext, cancellationToken);
                        break;
                    default:
                        this.telemetryClient.TrackTrace($"Received unexpected conversationType: {message.Conversation.ConversationType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error processing message: {ex.Message}");
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        // Sends the typing indicator to the user.
        private Task SendTypingIndicatorAsync(ITurnContext turnContext)
        {
            var typingActivity = turnContext.Activity.CreateReply();
            typingActivity.Type = ActivityTypes.Typing;
            return turnContext.SendActivityAsync(typingActivity);
        }

        /// <summary>
        /// Handles the members being added due to the conversationUpdate event in a teams scope.
        /// </summary>
        /// <param name="membersAdded">The members being added.</param>
        /// <param name="turnContext">The current turn context/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task OnMembersAddedToTeamAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            var teamDetails = ((JObject)turnContext.Activity.ChannelData).ToObject<TeamsChannelData>();
            var botDisplayName = turnContext.Activity.Recipient.Name;

            if (membersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                this.telemetryClient.TrackTrace($"Bot added to team {activity.Conversation.Id}");

                var properties = new Dictionary<string, string>
                {
                    { "Scope", activity.Conversation?.ConversationType },
                    { "TeamId", activity.Conversation?.Id },
                    { "InstallerId", activity.From.Id },
                };
                this.telemetryClient.TrackEvent("AppInstalled", properties);

                var teamMembers = await ((BotFrameworkAdapter)turnContext.Adapter).GetConversationMembersAsync(turnContext, cancellationToken);
                var personThatAddedBot = teamMembers.FirstOrDefault(x => x.Id == activity.From.Id)?.Name;

                var teamInstallInfo = new TeamInstallInfo()
                {
                    TeamId = teamDetails.Team.Id,
                    TenantId = teamDetails.Tenant.Id,
                    InstallerName = personThatAddedBot,
                    ServiceUrl = turnContext.Activity.ServiceUrl,
                    TeamName = teamDetails.Team.Name
                };
                await this.dataProvider.UpdateTeamInstallStatusAsync(teamInstallInfo, true);

                var teamWelcomeCardAttachment = WelcomeTeamCard.GetCard(teamDetails.Team.Name, activity.From?.Name, personThatAddedBot);
                await turnContext.SendActivityAsync(MessageFactory.Attachment(teamWelcomeCardAttachment));
            }
            else
            {
                this.telemetryClient.TrackTrace($"Adding {membersAdded.FirstOrDefault()?.Id} - {membersAdded.FirstOrDefault()?.Name} to {teamDetails.Team.Name}");
                var connectorClient = new ConnectorClient(new Uri(turnContext.Activity.ServiceUrl), this.microsoftAppCredentials);
                await this.SendUserWelcomeMessage(
                    connectorClient,
                    membersAdded.FirstOrDefault()?.Id,
                    teamDetails.Team.Name,
                    teamDetails.Team.Id,
                    teamDetails.Tenant.Id,
                    turnContext.Activity.Recipient.Id,
                    botDisplayName,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Handles message activity in the 1:1 chat (personal scope).
        /// </summary>
        /// <param name="message">The incoming message.</param>
        /// <param name="turnContext">The current turn/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task OnMessageActivityInPersonalChatAsync(
            IMessageActivity message,
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var senderAadId = turnContext.Activity.From.AadObjectId;
            var tenantId = turnContext.Activity.GetChannelData<TeamsChannelData>().Tenant.Id;
            var activityReply = ((Activity)turnContext.Activity).CreateReply();

            string text = (message.Text ?? string.Empty).Trim().ToLower();
            switch (text)
            {
                case "optout":
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted out");

                    var properties = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "false" },
                    };

                    this.telemetryClient.TrackEvent("UserOptInStatusSet", properties);

                    await this.dataProvider.SetUserInfoAsync(tenantId, senderAadId, false, turnContext.Activity.ServiceUrl);
                    activityReply.Attachments = new List<Attachment>()
                    {
                        new HeroCard()
                        {
                            Text = Resources.OptOutConfirmation,
                            Buttons = new List<CardAction>()
                            {
                                new CardAction()
                                {
                                    Title = Resources.ResumePairingsButtonText,
                                    DisplayText = Resources.ResumePairingsButtonText,
                                    Type = ActionTypes.MessageBack,
                                    Text = "optin"
                                }
                            }
                        }.ToAttachment(),
                    };
                    break;
                case "optin":
                    // User opted in
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted in");

                    var optInProps = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "true" },
                    };
                    this.telemetryClient.TrackEvent("UserOptInStatusSet", optInProps);

                    await this.dataProvider.SetUserInfoAsync(tenantId, senderAadId, true, turnContext.Activity.ServiceUrl);
                    activityReply.Attachments = new List<Attachment>()
                    {
                        new HeroCard()
                        {
                            Text = Resources.OptOutConfirmation,
                            Buttons = new List<CardAction>()
                            {
                                new CardAction()
                                {
                                    Title = Resources.PausePairingsButtonText,
                                    DisplayText = Resources.PausePairingsButtonText,
                                    Type = ActionTypes.MessageBack,
                                    Text = "optout"
                                }
                            }
                        }.ToAttachment(),
                    };
                    break;
                default:
                    this.telemetryClient.TrackTrace($"Cannot process the following: {turnContext.Activity.Text}");
                    activityReply.Attachments = new List<Attachment>
                    {
                        UnrecognizedInputCard.GetCard(),
                    };
                    break;
            }

            await turnContext.SendActivityAsync(activityReply, cancellationToken);
        }

        /// <summary>
        /// Handles the message activity in the channel/teams scope.
        /// </summary>
        /// <param name="turnContext">The current turn context/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task OnMessageActivityInChannelAsync(
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Attachment(UnrecognizedInputCard.GetCard()), cancellationToken);
        }

        /// <summary>
        /// When a new member is added to the team, they are to be welcomed by the bot.
        /// </summary>
        /// <param name="connectorClient">The connector client.</param>
        /// <param name="memberAddedId">Newly added team member.</param>
        /// <param name="teamName">The team name.</param>
        /// <param name="teamId">The teamID.</param>
        /// <param name="tenantId">The tenantID.</param>
        /// <param name="botId">The botID.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task SendUserWelcomeMessage(
            ConnectorClient connectorClient,
            string memberAddedId,
            string teamName,
            string teamId,
            string tenantId,
            string botId,
            string botDisplayName,
            CancellationToken cancellationToken)
        {
            try
            {
                var allMembers = await connectorClient.Conversations.GetConversationMembersAsync(teamId, cancellationToken);

                ChannelAccount userThatJustJoined = null;
                foreach (var m in allMembers)
                {
                    // both values are 29: values
                    if (m.Id == memberAddedId)
                    {
                        userThatJustJoined = m;
                        break;
                    }
                }

                if (userThatJustJoined != null)
                {
                    var installedTeam = await this.dataProvider.GetInstalledTeamAsync(teamId);
                    var userWelcomeCard = UserWelcomeCard.GetCard(installedTeam.InstallerName, botDisplayName, teamName);
                    await this.NotifyUser(
                        connectorClient,
                        userThatJustJoined,
                        userWelcomeCard,
                        botId,
                        tenantId,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"An error occurred: {ex.Message} - {ex.InnerException}");
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Method that will notify the user with the welcome card.
        /// </summary>
        /// <param name="connectorClient">The connector client.</param>
        /// <param name="userThatJustJoined">The newly added user.</param>
        /// <param name="attachmentToSend">The attachment to send to the user.</param>
        /// <param name="botId">The botID.</param>
        /// <param name="tenantId">The tenantID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<bool> NotifyUser(
            ConnectorClient connectorClient,
            ChannelAccount userThatJustJoined,
            Attachment attachmentToSend,
            string botId,
            string tenantId,
            CancellationToken cancellationToken)
        {
            try
            {
                // Ensuring that the conversation exists.
                var bot = new ChannelAccount { Id = botId };
                var conversationParameters = new ConversationParameters()
                {
                    Bot = bot,
                    Members = new List<ChannelAccount>()
                    {
                        userThatJustJoined,
                    },
                    TenantId = tenantId,
                };

                var response = await connectorClient.Conversations.CreateConversationAsync(conversationParameters, cancellationToken);
                var conversationId = response.Id;

                var activity = new Activity()
                {
                    Type = ActivityTypes.Message,
                    Attachments = new List<Attachment>()
                    {
                        attachmentToSend,
                    },
                };

                await connectorClient.Conversations.SendToConversationAsync(conversationId, activity, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error occurred while trying to notify {userThatJustJoined.Name}");
                this.telemetryClient.TrackException(ex);
                return false;
            }
        }

        private List<Tuple<ChannelAccount, ChannelAccount>> MakePairs(List<ChannelAccount> users)
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
            for (int i = 0; i < users.Count - 1; i += 2)
            {
                pairs.Add(new Tuple<ChannelAccount, ChannelAccount>(users[i], users[i + 1]));
            }

            return pairs;
        }

        private async Task<List<ChannelAccount>> GetOptedInUsers(ConnectorClient connectorClient, TeamInstallInfo teamInfo)
        {
            // Pull the roster of specified team and then remove everyone who has opted out explicitly
            var members = await connectorClient.Conversations.GetConversationMembersAsync(teamInfo.TeamId);
            this.telemetryClient.TrackTrace($"Found {members.Count} in team {teamInfo.TeamId}");

            var tasks = members.Select(m => this.dataProvider.GetUserInfoAsync(m.Properties["objectId"].ToString()));
            var results = await Task.WhenAll(tasks);

            return members
                .Zip(results, (member, userInfo) => ((userInfo == null) || userInfo.OptedIn) ? member : null)
                .Where(m => m != null)
                .ToList();
        }

        /// <summary>
        /// Notify a pairup.
        /// </summary>
        /// <param name="connectorClient">The connector client</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamName">The team name</param>
        /// <param name="botId">The bot id.</param>
        /// <param name="pair">The pairup</param>
        /// <returns>Number of users notified successfully</returns>
        private async Task<int> NotifyPair(ConnectorClient connectorClient, string tenantId, string teamName, string botId, Tuple<ChannelAccount, ChannelAccount> pair)
        {
            CancellationToken cancellationToken = default;
            this.telemetryClient.TrackTrace($"Sending pairup notification to {pair.Item1.Id} and {pair.Item2.Id}");

            var teamsPerson1 = pair.Item1;
            var teamsPerson2 = pair.Item2;

            // Fill in person2's info in the card for person1
            var cardForPerson1 = PairUpNotificationCard.GetCard(teamName, teamsPerson2, teamsPerson1, this.botDisplayName);

            // Fill in person1's info in the card for person2
            // var cardForPerson2 = PairUpNotificationCard.GetCard(teamName, teamsPerson1.Name, teamsPerson2.Name, teamsPerson1.GivenName, teamsPerson2.GivenName, teamsPerson2.GivenName, teamsPerson1.UserPrincipalName, this.botDisplayName);
            var cardForPerson2 = PairUpNotificationCard.GetCard(teamName, teamsPerson1, teamsPerson2, this.botDisplayName);

            // Send notifications and return the number that was successful
            var notifyResults = await Task.WhenAll(
                this.NotifyUser(connectorClient, teamsPerson1, cardForPerson2, botId, tenantId, cancellationToken),
                this.NotifyUser(connectorClient, teamsPerson2, cardForPerson1, botId, tenantId, cancellationToken));
            return notifyResults.Count(wasNotified => wasNotified);
        }

        /// <summary>
        /// Get the name of a team.
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <returns>The name of the team</returns>
        private async Task<string> GetTeamNameAsync(string teamId)
        {
            var teamDetailsResult = await this.dataProvider.GetInstalledTeamAsync(teamId);
            return teamDetailsResult.TeamName;
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