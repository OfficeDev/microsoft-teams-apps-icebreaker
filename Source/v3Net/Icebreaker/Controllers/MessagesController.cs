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
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
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
        private static TelemetryClient telemetryClient = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("APPINSIGHTS_INSTRUMENTATIONKEY")));

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

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task HandleMessageActivity(ConnectorClient connectorClient, Activity activity)
        {
            string replyText = null;
            var optOutRequest = false;

            if (activity.Value != null && ((dynamic)activity.Value).optout == true)
            {
                optOutRequest = true;
            }

            try
            {
                // Looking at the sender of the message
                var senderAadId = activity.From.AsTeamsChannelAccount().Properties["aadObjectId"].ToString();

                if (optOutRequest || string.Equals(activity.Text, "optout", StringComparison.InvariantCultureIgnoreCase))
                {
                    telemetryClient.TrackTrace($"Incoming user message: {activity.Text} from {senderAadId}");
                    await IcebreakerBot.OptOutUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, activity.ServiceUrl);

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

                    telemetryClient.TrackEvent("UserOptIn", optInEventProps);
                    await IcebreakerBot.OptInUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, activity.ServiceUrl);

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
                    var botName = CloudConfigurationManager.GetSetting("BotDisplayName");
                    telemetryClient.TrackTrace($"Cannot process the following: {activity.Text}");
                    replyText = Resources.IDontKnow;

                    var replyActivity = activity.CreateReply(replyText);
                    await connectorClient.Conversations.ReplyToActivityAsync(replyActivity);
                }
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                replyText = Resources.ErrorOccured;
            }
        }

        private async Task<Activity> HandleSystemActivity(ConnectorClient connectorClient, Activity message)
        {
            telemetryClient.TrackTrace("Processing system message");

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
                                telemetryClient.TrackTrace($"Bot installed to team {message.Conversation.Id}");

                                // The following lines are bringing in the installer of the application - which may be a team member
                                var teamMembers = await connectorClient.Conversations.GetConversationMembersAsync(message.Conversation.Id);

                                var personThatAddedBot = teamMembers.FirstOrDefault(x => x.Id == message.From.Id)?.Name;

                                // we were just added to team
                                await IcebreakerBot.SaveAddedToTeam(message.ServiceUrl, message.Conversation.Id, tenantId, personThatAddedBot);

                                await IcebreakerBot.WelcomeTeam(connectorClient, tenantId, message.Conversation.Id, personThatAddedBot);
                            }
                            else
                            {
                                telemetryClient.TrackTrace($"Adding a new member: {member.Id}");

                                // We are extracting the name of the app installer because it may not be a member
                                // of the team that a user is being added to
                                var installedTeam = IcebreakerBot.GetInstalledTeam(tenantId, teamsChannelData.Team.Id);

                                await IcebreakerBot.WelcomeUser(connectorClient, member.Id, tenantId, teamsChannelData.Team.Id, installedTeam.InstallerName);
                            }
                        }
                    }

                    if (message.MembersRemoved?.Any(x => x.Id == myBotId) == true)
                    {
                        telemetryClient.TrackTrace($"Bot removed from team {message.Conversation.Id}");

                        // we were just removed from a team
                        await IcebreakerBot.SaveRemoveFromTeam(message.ServiceUrl, message.Conversation.Id, tenantId);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                throw;
            }
        }
    }
}