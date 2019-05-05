//----------------------------------------------------------------------------------------------
// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams.Models;
    using Properties;

    /// <summary>
    /// Controller for the bot messaging endpoint
    /// </summary>
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly IcebreakerBot bot;
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagesController"/> class.
        /// </summary>
        /// <param name="bot">The Icebreaker bot instance</param>
        /// <param name="telemetryClient">The telemetry client instance</param>
        public MessagesController(IcebreakerBot bot, TelemetryClient telemetryClient)
        {
            this.bot = bot;
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// POST: api/messages
        /// Receive a message from a user and reply to it
        /// </summary>
        /// <param name="activity">The incoming activity</param>
        /// <returns>Task that resolves to the HTTP response message</returns>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            this.LogActivityTelemetry(activity);

            using (var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl)))
            {
                if (activity.Type == ActivityTypes.Message)
                {
                    await this.HandleMessageActivity(connectorClient, activity);
                }
                else
                {
                    await this.HandleSystemActivity(connectorClient, activity);
                }
            }

            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        private async Task HandleMessageActivity(ConnectorClient connectorClient, Activity activity)
        {
            try
            {
                var senderAadId = activity.From.Properties["aadObjectId"].ToString();
                var tenantId = activity.GetChannelData<TeamsChannelData>().Tenant.Id;

                if (string.Equals(activity.Text, "optout", StringComparison.InvariantCultureIgnoreCase))
                {
                    // User opted out
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted out");

                    var properties = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "false" },
                    };
                    this.telemetryClient.TrackEvent("UserOptInStausSet", properties);

                    await this.bot.OptOutUser(tenantId, senderAadId, activity.ServiceUrl);

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
                                    Text = "optin"
                                }
                            }
                        }.ToAttachment(),
                    };

                    await connectorClient.Conversations.ReplyToActivityAsync(optOutReply);
                }
                else if (string.Equals(activity.Text, "optin", StringComparison.InvariantCultureIgnoreCase))
                {
                    // User opted in
                    this.telemetryClient.TrackTrace($"User {senderAadId} opted in");

                    var properties = new Dictionary<string, string>
                    {
                        { "UserAadId", senderAadId },
                        { "OptInStatus", "true" },
                    };
                    this.telemetryClient.TrackEvent("UserOptInStatusSet", properties);

                    await this.bot.OptInUser(tenantId, senderAadId, activity.ServiceUrl);

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
                                    Text = "optout"
                                }
                            }
                        }.ToAttachment(),
                    };

                    await connectorClient.Conversations.ReplyToActivityAsync(optInReply);
                }
                else
                {
                    // Unknown input
                    this.telemetryClient.TrackTrace($"Cannot process the following: {activity.Text}");
                    var replyActivity = activity.CreateReply();
                    await this.bot.SendUnrecognizedInputMessage(connectorClient, replyActivity);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error while handling message activity: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }
        }

        private async Task HandleSystemActivity(ConnectorClient connectorClient, Activity message)
        {
            this.telemetryClient.TrackTrace("Processing system message");

            try
            {
                var teamsChannelData = message.GetChannelData<TeamsChannelData>();
                var tenantId = teamsChannelData.Tenant.Id;

                if (message.Type == ActivityTypes.ConversationUpdate)
                {
                    // conversation-update fires whenever a new 1:1 gets created between us and someone else as well
                    // only process the Teams ones.
                    if (string.IsNullOrEmpty(teamsChannelData?.Team?.Id))
                    {
                        // conversation-update is for 1:1 chat. Just ignore.
                        return;
                    }

                    string myBotId = message.Recipient.Id;
                    string teamId = message.Conversation.Id;

                    if (message.MembersAdded?.Count() > 0)
                    {
                        foreach (var member in message.MembersAdded)
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
                                var teamMembers = await connectorClient.Conversations.GetConversationMembersAsync(teamId);
                                var personThatAddedBot = teamMembers.FirstOrDefault(x => x.Id == message.From.Id)?.Name;

                                await this.bot.SaveAddedToTeam(message.ServiceUrl, teamId, tenantId, personThatAddedBot);
                                await this.bot.WelcomeTeam(connectorClient, teamId, personThatAddedBot);
                            }
                            else
                            {
                                this.telemetryClient.TrackTrace($"New member {member.Id} added to team {teamsChannelData.Team.Id}");

                                var installedTeam = await this.bot.GetInstalledTeam(teamsChannelData.Team.Id);
                                await this.bot.WelcomeUser(connectorClient, member.Id, tenantId, teamsChannelData.Team.Id, installedTeam.InstallerName);
                            }
                        }
                    }

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
                        await this.bot.SaveRemoveFromTeam(message.ServiceUrl, teamId, tenantId);
                    }
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error while handling system activity: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Log telemetry about the incoming activity.
        /// </summary>
        /// <param name="activity">The activity</param>
        private void LogActivityTelemetry(Activity activity)
        {
            var fromObjectId = activity.From?.Properties["aadObjectId"]?.ToString();
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
                { "Platform", clientInfoEntity?.Properties["platform"]?.ToString() }
            };
            this.telemetryClient.TrackEvent("UserActivity", properties);
        }
    }
}