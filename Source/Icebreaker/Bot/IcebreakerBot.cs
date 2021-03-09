// <copyright file="IcebreakerBot.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Helpers;
    using Icebreaker.Helpers.AdaptiveCards;
    using Icebreaker.Interfaces;
    using Icebreaker.Properties;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Implements the core logic for Icebreaker bot
    /// </summary>
    public class IcebreakerBot : TeamsActivityHandler
    {
        private readonly IBotDataProvider dataProvider;
        private readonly ConversationHelper conversationHelper;
        private readonly MicrosoftAppCredentials appCredentials;
        private readonly TelemetryClient telemetryClient;
        private readonly string botDisplayName;
        private readonly bool disableTenantFilter;
        private readonly HashSet<string> allowedTenantIds;

        /// <summary>
        /// Initializes a new instance of the <see cref="IcebreakerBot"/> class.
        /// </summary>
        /// <param name="dataProvider">The data provider to use</param>
        /// <param name="conversationHelper">Conversation helper instance to notify team members</param>
        /// <param name="appCredentials">Microsoft app credentials to use.</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public IcebreakerBot(IBotDataProvider dataProvider, ConversationHelper conversationHelper, MicrosoftAppCredentials appCredentials, TelemetryClient telemetryClient)
        {
            this.dataProvider = dataProvider;
            this.conversationHelper = conversationHelper;
            this.appCredentials = appCredentials;
            this.telemetryClient = telemetryClient;
            this.botDisplayName = CloudConfigurationManager.GetSetting("BotDisplayName");
            this.disableTenantFilter = Convert.ToBoolean(CloudConfigurationManager.GetSetting("DisableTenantFilter"), CultureInfo.InvariantCulture);
            var allowedTenants = CloudConfigurationManager.GetSetting("AllowedTenants");
            this.allowedTenantIds = allowedTenants
                ?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?.Select(p => p.Trim())
                .ToHashSet();
        }

        /// <summary>
        /// Handles an incoming activity.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// Reference link: https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.activityhandler.onturnasync?view=botbuilder-dotnet-stable.
        /// </remarks>
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                this.LogActivityTelemetry(turnContext.Activity);

                var isAllowedTenant = this.ValidateTenant(turnContext);
                if (!isAllowedTenant)
                {
                    return;
                }

                // Get the current culture info to use in resource files
                string locale = turnContext?.Activity.Entities?.FirstOrDefault(entity => entity.Type == "clientInfo")?.Properties["locale"]?.ToString();

                if (!string.IsNullOrEmpty(locale))
                {
                    CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(locale);
                }

                await base.OnTurnAsync(turnContext, cancellationToken);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// Invoked when a conversation update activity is received from the channel.
        /// Conversation update activities are useful when it comes to responding to users being added to or removed from the channel.
        /// For example, a bot could respond to a user being added by greeting the user.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// In a derived class, override this method to add logic that applies to all conversation update activities.
        /// </remarks>
        protected override async Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // conversation-update fires whenever a new 1:1 gets created between us and someone else as well
            // only process the Teams ones.
            var teamsChannelData = turnContext.Activity.GetChannelData<TeamsChannelData>();
            if (string.IsNullOrEmpty(teamsChannelData?.Team?.Id))
            {
                // conversation-update is for 1:1 chat. Just ignore.
                return;
            }

            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Provide logic for when members other than the bot
        /// join the conversation, such as your bot's welcome logic.
        /// </summary>
        /// <param name="membersAdded">A list of all the members added to the conversation, as
        /// described by the conversation update activity.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a conversation update activity that indicates one or more users other than the bot
        /// are joining the conversation, it calls this method.
        /// </remarks>
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if (membersAdded?.Count() > 0)
            {
                var message = turnContext.Activity;
                string myBotId = message.Recipient.Id;
                string teamId = message.Conversation.Id;
                var teamsChannelData = message.GetChannelData<TeamsChannelData>();

                foreach (var member in membersAdded)
                {
                    if (member.Id == myBotId)
                    {
                        this.telemetryClient.TrackTrace($"Bot installed to team {teamId}");

                        var properties = new Dictionary<string, string>
                                {
                                    { "Scope", message.Conversation?.ConversationType },
                                    { "TeamId", teamId },
                                    { "InstallerId", message.From.Id },
                                };
                        this.telemetryClient.TrackEvent("AppInstalled", properties);

                        // Try to determine the name of the person that installed the app, which is usually the sender of the message (From.Id)
                        // Note that in some cases we cannot resolve it to a team member, because the app was installed to the team programmatically via Graph
                        var personThatAddedBot = (await this.conversationHelper.GetMemberAsync(turnContext, message.From.Id, cancellationToken))?.Name;

                        await this.SaveAddedToTeam(message.ServiceUrl, teamId, teamsChannelData.Tenant.Id, personThatAddedBot);
                        await this.WelcomeTeam(turnContext, personThatAddedBot, cancellationToken);
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"New member {member.Id} added to team {teamsChannelData.Team.Id}");

                        await this.WelcomeUser(turnContext, member.Id, teamsChannelData.Tenant.Id, teamsChannelData.Team.Id, cancellationToken);
                    }
                }
            }

            await base.OnMembersAddedAsync(membersAdded, turnContext, cancellationToken);
        }

        /// <summary>
        /// Provide logic for when members other than the bot
        /// leave the conversation, such as your bot's good-bye logic.
        /// </summary>
        /// <param name="membersRemoved">A list of all the members removed from the conversation, as
        /// described by the conversation update activity.</param>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnConversationUpdateActivityAsync(ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
        /// method receives a conversation update activity that indicates one or more users other than the bot
        /// are leaving the conversation, it calls this method.
        /// </remarks>
        protected override async Task OnMembersRemovedAsync(IList<ChannelAccount> membersRemoved, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var message = turnContext.Activity;
            string myBotId = message.Recipient.Id;
            string teamId = message.Conversation.Id;
            var teamsChannelData = message.GetChannelData<TeamsChannelData>();
            if (message.MembersRemoved?.Any(x => x.Id == myBotId) == true)
            {
                this.telemetryClient.TrackTrace($"Bot removed from team {teamId}");

                var properties = new Dictionary<string, string>
                {
                    { "Scope", message.Conversation?.ConversationType },
                    { "TeamId", teamId },
                    { "UninstallerId", message.From.Id },
                };
                this.telemetryClient.TrackEvent("AppUninstalled", properties);

                // we were just removed from a team
                await this.SaveRemoveFromTeam(teamId, teamsChannelData.Tenant.Id);
            }

            await base.OnMembersRemovedAsync(membersRemoved, turnContext, cancellationToken);
        }

        /// <summary>
        /// Provide logic specific to
        /// <see cref="ActivityTypes.Message"/> activities, such as the conversational logic.
        /// Specifically the opt in and out operations.
        /// </summary>
        /// <param name="turnContext">A strongly-typed context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        /// <remarks>
        /// When the <see cref="OnTurnAsync(ITurnContext, CancellationToken)"/>
        /// method receives a message activity, it calls this method.
        /// </remarks>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await this.HandleMessageActivityAsync(turnContext, cancellationToken);
            await base.OnMessageActivityAsync(turnContext, cancellationToken);
        }

        /// <summary>
        /// Handle opt in/out operations by updating user preference in data store.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task HandleMessageActivityAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            try
            {
                var activity = turnContext.Activity;
                var senderAadId = activity.From.AadObjectId;
                var tenantId = activity.GetChannelData<TeamsChannelData>().Tenant.Id;

                if (string.Equals(activity.Text, MatchingActions.OptOut, StringComparison.InvariantCultureIgnoreCase))
                {
                    // User opted out
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted out");

                    var properties = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "false" },
                    };
                    this.telemetryClient.TrackEvent("UserOptInStatusSet", properties);

                    await this.OptOutUser(tenantId, senderAadId, activity.ServiceUrl);

                    var optOutReply = activity.CreateReply();
                    optOutReply.Attachments = new List<Attachment>
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
                                    Text = MatchingActions.OptIn,
                                },
                            },
                        }.ToAttachment(),
                    };

                    await turnContext.SendActivityAsync(optOutReply, cancellationToken).ConfigureAwait(false);
                }
                else if (string.Equals(activity.Text, MatchingActions.OptIn, StringComparison.InvariantCultureIgnoreCase))
                {
                    // User opted in
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted in");

                    var properties = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "true" },
                    };
                    this.telemetryClient.TrackEvent("UserOptInStatusSet", properties);

                    await this.OptInUser(tenantId, senderAadId, activity.ServiceUrl);

                    var optInReply = activity.CreateReply();
                    optInReply.Attachments = new List<Attachment>
                    {
                        new HeroCard()
                        {
                            Text = Resources.OptInConfirmation,
                            Buttons = new List<CardAction>()
                            {
                                new CardAction()
                                {
                                    Title = Resources.PausePairingsButtonText,
                                    DisplayText = Resources.PausePairingsButtonText,
                                    Type = ActionTypes.MessageBack,
                                    Text = MatchingActions.OptOut,
                                },
                            },
                        }.ToAttachment(),
                    };

                    await turnContext.SendActivityAsync(optInReply, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // Unknown input
                    this.telemetryClient.TrackTrace($"Cannot process the following: {activity.Text}");
                    var replyActivity = activity.CreateReply();
                    await this.SendUnrecognizedInputMessageAsync(turnContext, replyActivity, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error while handling message activity: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// Method that will return the information of the installed team
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <returns>The team that the bot has been installed to</returns>
        private Task<TeamInstallInfo> GetInstalledTeam(string teamId)
        {
            return this.dataProvider.GetInstalledTeamAsync(teamId);
        }

        /// <summary>
        /// Send a welcome message to the user that was just added to a team.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="memberAddedId">The id of the added user</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="teamId">The id of the team the user was added to</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Tracking task</returns>
        private async Task WelcomeUser(ITurnContext turnContext, string memberAddedId, string tenantId, string teamId, CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"Sending welcome message for user {memberAddedId}");

            var installedTeam = await this.GetInstalledTeam(teamId);
            var teamName = turnContext.Activity.TeamsGetTeamInfo().Name;
            ChannelAccount userThatJustJoined = await this.conversationHelper.GetMemberAsync(turnContext, memberAddedId, cancellationToken);

            if (userThatJustJoined != null)
            {
                var welcomeMessageCard = WelcomeNewMemberAdaptiveCard.GetCard(teamName, userThatJustJoined.Name, this.botDisplayName, installedTeam.InstallerName);
                await this.conversationHelper.NotifyUserAsync(turnContext, MessageFactory.Attachment(welcomeMessageCard), userThatJustJoined, tenantId, cancellationToken);
            }
            else
            {
                this.telemetryClient.TrackTrace($"Member {memberAddedId} was not found in team {teamId}, skipping welcome message.", SeverityLevel.Warning);
            }
        }

        /// <summary>
        /// Sends a welcome message to the General channel of the team that this bot has been installed to
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="botInstaller">The installer of the application</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Tracking task</returns>
        private async Task WelcomeTeam(ITurnContext turnContext, string botInstaller, CancellationToken cancellationToken)
        {
            var teamId = turnContext.Activity.Conversation.Id;
            this.telemetryClient.TrackTrace($"Sending welcome message for team {teamId}");

            var teamName = turnContext.Activity.TeamsGetTeamInfo().Name;
            var welcomeTeamMessageCard = WelcomeTeamAdaptiveCard.GetCard(teamName, botInstaller);
            await this.NotifyTeamAsync(turnContext, MessageFactory.Attachment(welcomeTeamMessageCard), teamId, cancellationToken);
        }

        /// <summary>
        /// Sends a message whenever there is unrecognized input into the bot
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="replyActivity">The activity for replying to a message</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>Tracking task</returns>
        private async Task SendUnrecognizedInputMessageAsync(ITurnContext turnContext, Activity replyActivity, CancellationToken cancellationToken)
        {
            replyActivity.Attachments = new List<Attachment> { UnrecognizedInputAdaptiveCard.GetCard() };
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        /// <summary>
        /// Save information about the team to which the bot was added.
        /// </summary>
        /// <param name="serviceUrl">The service url</param>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="botInstaller">Person that has added the bot to the team</param>
        /// <returns>Tracking task</returns>
        private Task SaveAddedToTeam(string serviceUrl, string teamId, string tenantId, string botInstaller)
        {
            var teamInstallInfo = new TeamInstallInfo
            {
                ServiceUrl = serviceUrl,
                TeamId = teamId,
                TenantId = tenantId,
                InstallerName = botInstaller,
            };
            return this.dataProvider.UpdateTeamInstallStatusAsync(teamInstallInfo, true);
        }

        /// <summary>
        /// Save information about the team from which the bot was removed.
        /// </summary>
        /// <param name="teamId">The team id</param>
        /// <param name="tenantId">The tenant id</param>
        /// <returns>Tracking task</returns>
        private Task SaveRemoveFromTeam(string teamId, string tenantId)
        {
            var teamInstallInfo = new TeamInstallInfo
            {
                TeamId = teamId,
                TenantId = tenantId,
            };
            return this.dataProvider.UpdateTeamInstallStatusAsync(teamInstallInfo, false);
        }

        /// <summary>
        /// Opt out the user from further pairups
        /// </summary>
        /// <param name="tenantId">The tenant id</param>
        /// <param name="userId">The user id</param>
        /// <param name="serviceUrl">The service url</param>
        /// <returns>Tracking task</returns>
        private Task OptOutUser(string tenantId, string userId, string serviceUrl)
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
        private Task OptInUser(string tenantId, string userId, string serviceUrl)
        {
            return this.dataProvider.SetUserInfoAsync(tenantId, userId, true, serviceUrl);
        }

        /// <summary>
        /// Method that will send out the message in the General channel of the team
        /// that this bot has been installed to
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="activity">The actual welcome card (for the team)</param>
        /// <param name="teamId">The team id</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A tracking task</returns>
        private async Task NotifyTeamAsync(ITurnContext turnContext, IMessageActivity activity, string teamId, CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"Sending notification to team {teamId}");

            try
            {
                activity.Conversation = new ConversationAccount()
                {
                    Id = teamId,
                };

                var conversationParameters = new ConversationParameters
                {
                    Activity = (Activity)activity,
                    ChannelData = new TeamsChannelData { Channel = new ChannelInfo(teamId) },
                };

                await ((BotFrameworkAdapter)turnContext.Adapter).CreateConversationAsync(
                    null,
                    turnContext.Activity.ServiceUrl,
                    this.appCredentials,
                    conversationParameters,
                    null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error sending notification to team: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }
        }

        /// <summary>
        /// Log telemetry about the incoming activity.
        /// </summary>
        /// <param name="activity">The activity</param>
        private void LogActivityTelemetry(Activity activity)
        {
            var fromObjectId = activity.From?.AadObjectId;
            var clientInfoEntity = activity.Entities?.Where(e => e.Type == "clientInfo")?.FirstOrDefault();
            var channelData = activity.GetChannelData<TeamsChannelData>();

            var properties = new Dictionary<string, string>
            {
                { "ActivityId", activity.Id },
                { "ActivityType", activity.Type },
                { "UserAadObjectId", fromObjectId },
                {
                    "ConversationType",
                    string.IsNullOrWhiteSpace(activity.Conversation?.ConversationType) ? "personal" : activity.Conversation.ConversationType
                },
                { "ConversationId", activity.Conversation?.Id },
                { "TeamId", channelData?.Team?.Id },
                { "Locale", clientInfoEntity?.Properties["locale"]?.ToString() },
                { "Platform", clientInfoEntity?.Properties["platform"]?.ToString() },
            };
            this.telemetryClient.TrackEvent("UserActivity", properties);
        }

        private bool ValidateTenant(ITurnContext turnContext)
        {
            if (this.disableTenantFilter)
            {
                return true;
            }

            if (this.allowedTenantIds == null || !this.allowedTenantIds.Any())
            {
                var exceptionMessage = "AllowedTenants setting is not set properly in the configuration file.";
                throw new ApplicationException(exceptionMessage);
            }

            var tenantId = turnContext?.Activity?.Conversation?.TenantId;
            return this.allowedTenantIds.Contains(tenantId);
        }
    }
}