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
    using Microsoft.Bot.Connector.Teams;
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
                // Looking at the sender of the message
                var senderAadId = activity.From.AsTeamsChannelAccount().Properties["aadObjectId"].ToString();

                if ((((dynamic)activity?.Value)?.optout == true) ||
                    string.Equals(activity.Text, "optout", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.telemetryClient.TrackTrace($"Incoming user message: {activity.Text} from {senderAadId}");
                    await this.bot.OptOutUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, activity.ServiceUrl);

                    var optInReply = activity.CreateReply();
                    optInReply.Attachments = new List<Attachment>();
                    var optOutCard = new HeroCard()
                    {
                        Text = Resources.OptOutConfirmation,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = Resources.ResumePairingsButtonText,
                                Type = ActionTypes.MessageBack,
                                Text = "optin"
                            }
                        }
                    };
                    optInReply.Attachments.Add(optOutCard.ToAttachment());

                    await connectorClient.Conversations.ReplyToActivityAsync(optInReply);
                }
                else if (string.Equals(activity.Text, "optin", StringComparison.InvariantCultureIgnoreCase))
                {
                    Dictionary<string, string> optInEventProps = new Dictionary<string, string>()
                        {
                            { "message", activity.Text },
                            { "messageSender", senderAadId },
                            { "messageTimeStamp", DateTime.Now.ToString() }
                        };

                    this.telemetryClient.TrackEvent("UserOptIn", optInEventProps);
                    await this.bot.OptInUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, activity.ServiceUrl);

                    var optOutReply = activity.CreateReply();
                    optOutReply.Attachments = new List<Attachment>();
                    var optOutCard = new HeroCard()
                    {
                        Text = Resources.OptInConfirmation,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = Resources.PausePairingsButtonText,
                                Type = ActionTypes.MessageBack,
                                Text = "optout"
                            }
                        }
                    };
                    optOutReply.Attachments.Add(optOutCard.ToAttachment());

                    await connectorClient.Conversations.ReplyToActivityAsync(optOutReply);
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Cannot process the following: {activity.Text}");

                    var replyActivity = activity.CreateReply(Resources.IDontKnow);
                    await connectorClient.Conversations.ReplyToActivityAsync(replyActivity);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error while handling message activity: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
            }
        }

        private async Task<Activity> HandleSystemActivity(ConnectorClient connectorClient, Activity message)
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
                        return null;
                    }

                    string myBotId = message.Recipient.Id;

                    if (message.MembersAdded?.Count() > 0)
                    {
                        foreach (var member in message.MembersAdded)
                        {
                            if (member.Id == myBotId)
                            {
                                this.telemetryClient.TrackTrace($"Bot installed to team {message.Conversation.Id}");

                                // Try to determine the name of the person that installed the app, which is usually the sender of the message (From.Id)
                                // Note that in some cases we cannot resolve it to a team member, because the app was installed to the team programmatically via Graph
                                var teamMembers = await connectorClient.Conversations.GetConversationMembersAsync(message.Conversation.Id);
                                var personThatAddedBot = teamMembers.FirstOrDefault(x => x.Id == message.From.Id)?.Name;

                                await this.bot.SaveAddedToTeam(message.ServiceUrl, message.Conversation.Id, tenantId, personThatAddedBot);
                                await this.bot.WelcomeTeam(connectorClient, tenantId, message.Conversation.Id, personThatAddedBot);
                            }
                            else
                            {
                                this.telemetryClient.TrackTrace($"Adding a new member: {member.Id}");

                                var installedTeam = await this.bot.GetInstalledTeam(tenantId, teamsChannelData.Team.Id);
                                await this.bot.WelcomeUser(connectorClient, member.Id, tenantId, teamsChannelData.Team.Id, installedTeam.InstallerName);
                            }
                        }
                    }

                    if (message.MembersRemoved?.Any(x => x.Id == myBotId) == true)
                    {
                        this.telemetryClient.TrackTrace($"Bot removed from team {message.Conversation.Id}");

                        // we were just removed from a team
                        await this.bot.SaveRemoveFromTeam(message.ServiceUrl, message.Conversation.Id, tenantId);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error while handling system activity: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}