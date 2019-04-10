//----------------------------------------------------------------------------------------------
// <copyright file="MessagesController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using Properties;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;
    using Microsoft.Azure;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Connector.Teams;
    using Microsoft.Bot.Connector.Teams.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public static TelemetryClient telemetry = new TelemetryClient(new TelemetryConfiguration(CloudConfigurationManager.GetSetting("AppInsightsInstrumentationKey")));
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
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
                        telemetry.TrackTrace($"Incoming user message: {activity.Text} from {senderAadId} at {DateTime.Now.ToString()}");
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

                        telemetry.TrackEvent("UserOptIn", optInEventProps); 
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

                        telemetry.TrackEvent("FeedbackEvent", feedbackEventProps); 
                        replyText = $"If you want to provide feedback about me, contact my creator at {emailAddress}";
                    }
                    else
                    {
                        var botName = CloudConfigurationManager.GetSetting("BotDisplayName");
                        telemetry.TrackTrace($"Cannot process the following: {activity.Text}"); 
                        replyText = Resources.IDontKnow;
                    }
                }
                catch (Exception ex)
                {
                    telemetry.TrackException(ex);
                    replyText = Resources.ErrorOccured;
                }

                using (var connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl)))
                {
                    var replyActivity = activity.CreateReply(replyText);
                    await connectorClient.Conversations.ReplyToActivityAsync(replyActivity);
                }
            }
            else
            {
                await HandleSystemMessage(activity);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            telemetry.TrackTrace("Processing system message");

            try
            {
                var channelData = message.GetChannelData<TeamsChannelData>();

                if (message.Type == ActivityTypes.ConversationUpdate)
                {
                    // conversation-update fires whenever a new 1:1 gets created between us and someone else as well
                    // only process the Teams ones.
                    var teamsChannelData = message.GetChannelData<TeamsChannelData>();

                    if (teamsChannelData.Team == null || string.IsNullOrEmpty(teamsChannelData?.Team?.Id))
                    {
                        // conversation-update is for 1:1 chat. Just ignore.
                        return null;
                    }

                    string memberAddedId = string.Empty;
                    if (message.MembersAdded.Count > 0)
                    {
                        memberAddedId = message.MembersAdded.First().Id;
                    }

                    string memberRemovedId = string.Empty;
                    if (message.MembersRemoved.Count > 0)
                    {
                        memberRemovedId = message.MembersRemoved.First().Id;
                    }

                    string myId = message.Recipient.Id;

                    if (memberAddedId.Equals(myId))
                    {
                        telemetry.TrackTrace($"Adding a new member: {memberAddedId}"); 

                        // we were just added to team   
                        await IcebreakerBot.SaveAddedToTeam(message.ServiceUrl, message.Conversation.Id, channelData.Tenant.Id);

                        // TODO: post activity.from has who added the bot. Can record it in schema.
                    }
                    else if (memberRemovedId.Equals(myId))
                    {
                        // we were just removed from a team
                        await IcebreakerBot.SaveRemoveFromTeam(message.ServiceUrl, message.Conversation.Id, channelData.Tenant.Id);
                    }
                    else if (!string.IsNullOrEmpty(memberAddedId)) // If I wasn't added or removed, then someome else must have been added to team
                    {
                        telemetry.TrackTrace($"Adding a new member: {memberAddedId}"); 

                        // someone else was added send them a welcome message
                        await IcebreakerBot.WelcomeUser(message.ServiceUrl, memberAddedId, channelData.Tenant.Id, channelData.Team.Id);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                telemetry.TrackException(ex);
                throw;
            }
        }
    }
}