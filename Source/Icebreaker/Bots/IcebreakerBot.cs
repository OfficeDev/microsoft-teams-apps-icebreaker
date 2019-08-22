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
    using Icebreaker.Helpers;
    using Icebreaker.Helpers.AdaptiveCards;
    using Microsoft.ApplicationInsights;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector.Authentication;
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
                        case "personal":
                            await this.OnMembersAddedToPersonalChatAsync(activity.MembersAdded, turnContext, cancellationToken);
                            break;
                        case "channel":
                            await this.OnMembersAddedToTeamAsync(activity.MembersAdded, turnContext, cancellationToken);
                            break;
                        default:
                            this.telemetryClient.TrackTrace($"Ignoring event from the conversation type: {activity.Conversation.ConversationType}");
                            break;
                    }
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
                var message = turnContext.Activity;

                this.telemetryClient.TrackTrace($"Received message activity");
                this.telemetryClient.TrackTrace($"from: {message.From?.Id}conversation: {message.Conversation.Id}, replyToId: {message.ReplyToId}");

                await this.SendTypingIndicatorAsync(turnContext);

                switch (message.Conversation.ConversationType)
                {
                    case "personal":
                        await this.OnMessageActivityInPersonalChatAsync(message, turnContext, cancellationToken);
                        break;
                    case "channel":
                        await this.OnMessageActivityInChannelAsync(message, turnContext, cancellationToken);
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
        /// Handles the members being added due to the conversationUpdate event in a 1:1 chat.
        /// </summary>
        /// <param name="membersAdded">The members being added.</param>
        /// <param name="turnContext">The current turn context/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task OnMembersAddedToPersonalChatAsync(
            IList<ChannelAccount> membersAdded,
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            if (membersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                // User started chatting with the bot in the personal scope - for the first time.
                this.telemetryClient.TrackTrace($"Bot added to 1:1 chat {activity.Conversation.Id}");

                await turnContext.SendActivityAsync(MessageFactory.Text("You will get your welcome card"));
            }
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
            if (membersAdded.Any(m => m.Id == activity.Recipient.Id))
            {
                this.telemetryClient.TrackTrace($"Bot added to team {activity.Conversation.Id}");

                var teamDetails = ((JObject)turnContext.Activity.ChannelData).ToObject<TeamsChannelData>();
                var botDisplayName = turnContext.Activity.Recipient.Name;

                var teamWelcomeCardString = WelcomeTeamAdaptiveCard.GetCard(teamDetails.Team.Name, activity.From?.Name, string.Empty);
                await turnContext.SendActivityAsync(MessageFactory.Text($"Hello {teamDetails.Team.Name}"));

                // await this.SendCardToTeamAsync(turnContext, teamWelcomeCardAttachment, teamDetails.Team.Id, cancellationToken);
                // await turnContext.SendActivityAsync(MessageFactory.Attachment(teamWelcomeCardAttachment));
            }
        }

        /// <summary>
        /// Sending the cards to the team which would be receiving the team related notifications.
        /// </summary>
        /// <param name="turnContext">The current turn/execution flow.</param>
        /// <param name="cardToSend">The card to send.</param>
        /// <param name="teamId">The teamId to send notifications to.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution that contains the ConversationResourceResponse.</returns>
        private async Task<ConversationResourceResponse> SendCardToTeamAsync(
            ITurnContext turnContext,
            Attachment cardToSend,
            string teamId,
            CancellationToken cancellationToken)
        {
            var conversationParameters = new ConversationParameters
            {
                Activity = (Activity)MessageFactory.Attachment(cardToSend),
                ChannelData = new TeamsChannelData { Channel = new ChannelInfo(teamId), },
            };

            var tcs = new TaskCompletionSource<ConversationResourceResponse>();
            await ((BotFrameworkAdapter)turnContext.Adapter).CreateConversationAsync(
                null, // If we set channel = "msteams", there is an error as preinstalled middleware expects ChannelData to be present
                turnContext.Activity.ServiceUrl,
                this.microsoftAppCredentials,
                conversationParameters,
                (newTurnContext, newCancellationToken) =>
                {
                    var activity = newTurnContext.Activity;
                    tcs.SetResult(new ConversationResourceResponse
                    {
                        Id = activity.Conversation.Id,
                        ActivityId = activity.Id,
                        ServiceUrl = activity.ServiceUrl,
                    });
                    return Task.CompletedTask;
                },
                cancellationToken);

            return await tcs.Task;
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
            if (!string.IsNullOrEmpty(message.ReplyToId) &&
                message.Value != null &&
                ((JObject)message.Value).HasValues)
            {
                this.telemetryClient.TrackTrace("Card submit in 1:1 chat");

                // await this.OnAdaptiveCardSubmitInPersonalChatAsync(message, turnContext, cancellationToken);
                return;
            }

            string text = (message.Text ?? string.Empty).Trim().ToLower();
            await turnContext.SendActivityAsync(MessageFactory.Text(text));
        }

        /// <summary>
        /// Handles the message activity in the channel/teams scope.
        /// </summary>
        /// <param name="message">The current activity.</param>
        /// <param name="turnContext">The current turn context/execution flow.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A unit of execution.</returns>
        private async Task OnMessageActivityInChannelAsync(
            IMessageActivity message,
            ITurnContext<IMessageActivity> turnContext,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(message.ReplyToId) &&
                message.Value != null &&
                ((JObject)message.Value).HasValues)
            {
                this.telemetryClient.TrackTrace("Card submit in channel");

                // await this.OnAdaptiveCardSubmitInChannelAsync(message, turnContext, cancellationToken);
                return;
            }

            string text = (message.Text ?? string.Empty).Trim().ToLower();
            await turnContext.SendActivityAsync(MessageFactory.Text($"Yahtzee: {text}"));
        }
    }
}