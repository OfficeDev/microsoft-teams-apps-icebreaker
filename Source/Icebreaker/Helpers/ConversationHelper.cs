// <copyright file="ConversationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Azure;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Contains shared logic to notify team members
    /// </summary>
    public class ConversationHelper
    {
        private readonly MicrosoftAppCredentials appCredentials;
        private readonly TelemetryClient telemetryClient;
        private readonly string botId;
        private readonly bool isTesting;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationHelper"/> class.
        /// </summary>
        /// <param name="appCredentials">Microsoft app credentials to use.</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        public ConversationHelper(MicrosoftAppCredentials appCredentials, TelemetryClient telemetryClient)
        {
            this.appCredentials = appCredentials;
            this.telemetryClient = telemetryClient;
            this.botId = CloudConfigurationManager.GetSetting("MicrosoftAppId");
            this.isTesting = Convert.ToBoolean(CloudConfigurationManager.GetSetting("Testing"));
        }

        /// <summary>
        /// Send a card to a user in direct conversation
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="cardToSend">The actual welcome card (for the team)</param>
        /// <param name="user">User channel account</param>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>True/False operation status</returns>
        public async Task<bool> NotifyUserAsync(ITurnContext turnContext, IMessageActivity cardToSend, ChannelAccount user, string tenantId, CancellationToken cancellationToken)
        {
            var botFrameworkAdapter = (BotFrameworkHttpAdapter)turnContext.Adapter;
            var teamsChannelId = turnContext.Activity.TeamsGetChannelId();
            var serviceUrl = turnContext.Activity.ServiceUrl;

            return await this.NotifyUserAsync(botFrameworkAdapter, serviceUrl, teamsChannelId, cardToSend, user, tenantId, cancellationToken);
        }

        /// <summary>
        /// Send a card to a user in direct conversation
        /// </summary>
        /// <param name="botFrameworkAdapter">Bot adapter</param>
        /// <param name="serviceUrl">Service url</param>
        /// <param name="teamsChannelId">Team channel id where the bot is installed</param>
        /// <param name="cardToSend">The actual welcome card (for the team)</param>
        /// <param name="user">User channel account</param>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>True/False operation status</returns>
        public async Task<bool> NotifyUserAsync(BotFrameworkHttpAdapter botFrameworkAdapter, string serviceUrl, string teamsChannelId, IMessageActivity cardToSend, ChannelAccount user, string tenantId, CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"Sending notification to user {user.Id}");

            try
            {
                // conversation parameters
                var conversationParameters = new ConversationParameters
                {
                    Bot = new ChannelAccount { Id = this.botId },
                    Members = new[] { user },
                    ChannelData = new TeamsChannelData
                    {
                        Tenant = new TenantInfo(tenantId),
                    }
                };

                if (!this.isTesting)
                {
                    // shoot the activity over
                    await botFrameworkAdapter.CreateConversationAsync(
                        teamsChannelId,
                        serviceUrl,
                        this.appCredentials,
                        conversationParameters,
                        async (newTurnContext, newCancellationToken) =>
                        {
                            // Get the conversationReference
                            var conversationReference = newTurnContext.Activity.GetConversationReference();

                            // Send the proactive welcome message
                            await botFrameworkAdapter.ContinueConversationAsync(
                                this.appCredentials.MicrosoftAppId,
                                conversationReference,
                                async (conversationTurnContext, conversationCancellationToken) =>
                                {
                                    await conversationTurnContext.SendActivityAsync(cardToSend, conversationCancellationToken);
                                },
                                cancellationToken);
                        },
                        cancellationToken).ConfigureAwait(false);
                }

                return true;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"Error sending notification to user: {ex.Message}", SeverityLevel.Warning);
                this.telemetryClient.TrackException(ex);
                return false;
            }
        }
    }
}