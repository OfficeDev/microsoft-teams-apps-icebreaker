﻿//----------------------------------------------------------------------------------------------
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
                    telemetryClient.TrackTrace($"Incoming user message: {activity.Text} from {senderAadId} at {DateTime.Now.ToString()}");
                    await IcebreakerBot.OptOutUser(activity.GetChannelData<TeamsChannelData>().Tenant.Id, senderAadId, activity.ServiceUrl);
                    replyText = Resources.OptOutConfirmation;
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

                    replyText = Resources.OptInConfirmation;
                }
                else if (string.Equals(activity.Text, "feedback", StringComparison.InvariantCultureIgnoreCase))
                {
                    string emailAddress = CloudConfigurationManager.GetSetting("ContactEmail");
                    Dictionary<string, string> feedbackEventProps = new Dictionary<string, string>()
                        {
                            { "message", activity.Text },
                            { "messageSender", senderAadId },
                            { "contactEmail", emailAddress },
                            { "messageTimeStamp", DateTime.Now.ToString() }
                        };

                    telemetryClient.TrackEvent("FeedbackEvent", feedbackEventProps);
                    replyText = $"If you want to provide feedback about me, contact my creator at {emailAddress}";
                }
                else
                {
                    var botName = CloudConfigurationManager.GetSetting("BotDisplayName");
                    telemetryClient.TrackTrace($"Cannot process the following: {activity.Text}");
                    replyText = Resources.IDontKnow;
                }
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
                replyText = Resources.ErrorOccured;
            }

            var replyActivity = activity.CreateReply(replyText);
            await connectorClient.Conversations.ReplyToActivityAsync(replyActivity);
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

                                // we were just added to team
                                await IcebreakerBot.SaveAddedToTeam(message.ServiceUrl, message.Conversation.Id, tenantId);

                                // TODO: post activity.from has who added the bot. Can record it in schema.
                            }
                            else
                            {
                                // Someome else must have been added to team, send them a welcome message
                                telemetryClient.TrackTrace($"Adding a new member: {member.Id}");

                                await IcebreakerBot.WelcomeUser(connectorClient, member.Id, tenantId, teamsChannelData.Team.Id);
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