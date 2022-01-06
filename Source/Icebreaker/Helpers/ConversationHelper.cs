// <copyright file="ConversationHelper.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Icebreaker.Interfaces;
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.DataContracts;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Logging;
    using Polly.Retry;

    /// <summary>
    /// Contains shared logic to notify team members
    /// </summary>
    public class ConversationHelper
    {
        private const string MsTeamsBotFrameworkChannelId = "msteams";
        private readonly IAppSettings appSettings;
        private readonly ISecretsProvider secretsProvider;
        private readonly TelemetryClient telemetryClient;
        private readonly AsyncRetryPolicy retryPolicy;
        private readonly ILogger<ConversationHelper> logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationHelper"/> class.
        /// </summary>
        /// <param name="appSettings">App Settings.</param>
        /// <param name="secretsProvider">To fetch secrets</param>
        /// <param name="telemetryClient">The telemetry client to use</param>
        /// <param name="logger">logger to use</param>
        public ConversationHelper(
            IAppSettings appSettings,
            ISecretsProvider secretsProvider,
            TelemetryClient telemetryClient,
            ILogger<ConversationHelper> logger)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.secretsProvider = secretsProvider ?? throw new ArgumentNullException(nameof(secretsProvider));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(this.telemetryClient));
            this.logger = logger;
            this.retryPolicy = RetryPolicyHelper.GetRetryPolicy(logger: this.logger);
            this.telemetryClient.TrackTrace("Allowed tenant ids "+ this.appSettings.AllowedTenantIds.Count.ToString());
            this.telemetryClient.TrackTrace("Allowed tenant ids " + string.Join("," , this.appSettings.AllowedTenantIds));

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
            var botAdapter = turnContext.Adapter;
            var serviceUrl = turnContext.Activity.ServiceUrl;
            this.telemetryClient.TrackTrace("tenant id " + tenantId);
           
            return await this.NotifyUserAsync(botAdapter, serviceUrl, cardToSend, user, tenantId, cancellationToken);
        }

        /// <summary>
        /// Send a card to a user in direct conversation
        /// </summary>
        /// <param name="botFrameworkAdapter">Bot adapter</param>
        /// <param name="serviceUrl">Service url</param>
        /// <param name="cardToSend">The actual welcome card (for the team)</param>
        /// <param name="user">User channel account</param>
        /// <param name="tenantId">Tenant id</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>True/False operation status</returns>
        public async Task<bool> NotifyUserAsync(BotAdapter botFrameworkAdapter, string serviceUrl, IMessageActivity cardToSend, ChannelAccount user, string tenantId, CancellationToken cancellationToken)
        {
            this.telemetryClient.TrackTrace($"Sending notification to user {user.Id}");

            try
            {
                // conversation parameters
                var conversationParameters = new ConversationParameters
                {
                    Bot = new ChannelAccount { Id = this.appSettings.MicrosoftAppId },
                    Members = new[] { user },
                    ChannelData = new TeamsChannelData
                    {
                        Tenant = new TenantInfo(tenantId),
                    },
                };

                var appCredentials = await this.secretsProvider.GetAppCredentialsAsync();
                if (!this.appSettings.IsTesting)
                {
                    await this.retryPolicy.ExecuteAsync(async () =>
                    {
                        try
                        {
                            // shoot the activity over
                            await ((BotFrameworkAdapter)botFrameworkAdapter).CreateConversationAsync(
                                MsTeamsBotFrameworkChannelId,
                                serviceUrl,
                                appCredentials,
                                conversationParameters,
                                async (newTurnContext, newCancellationToken) =>
                                {
                            // Get the conversationReference
                            var conversationReference = newTurnContext.Activity.GetConversationReference();

                            await botFrameworkAdapter.ContinueConversationAsync(
                                this.appSettings.MicrosoftAppId,
                                conversationReference,
                                async (conversationTurnContext, conversationCancellationToken) =>
                                {
                                    await conversationTurnContext.SendActivityAsync(cardToSend, conversationCancellationToken);
                                },
                                cancellationToken);
                                },
                                cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            this.logger.LogError(ex, $"Error sending notification to user: {user.Id}.");
                            throw;
                        }
                    });
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

        /// <summary>
        /// Gets the account of a single conversation member.
        /// This works in one-on-one, group, and teams scoped conversations.
        /// </summary>
        /// <param name="turnContext"> Turn context. </param>
        /// <param name="memberId"> ID of the user in question. </param>
        /// <param name="cancellationToken"> cancellation token. </param>
        /// <returns>Team Details.</returns>
        public virtual async Task<TeamsChannelAccount> GetMemberAsync(ITurnContext turnContext, string memberId, CancellationToken cancellationToken)
        {
            return await TeamsInfo.GetMemberAsync(turnContext, memberId, cancellationToken);
        }

        /// <summary>
        /// Gets the details for the given team id. This only works in teams scoped conversations.
        /// </summary>
        /// <param name="turnContext"> Turn context. </param>
        /// <param name="teamId"> The id of the Teams team. </param>
        /// <param name="cancellationToken"> Cancellation token. </param>
        /// <returns>Team Details.</returns>
        public virtual async Task<TeamDetails> GetTeamDetailsAsync(ITurnContext turnContext, string teamId, CancellationToken cancellationToken)
        {
            return await TeamsInfo.GetTeamDetailsAsync(turnContext, teamId, cancellationToken);
        }

        /// <summary>
        /// Get the name of a team.
        /// </summary>
        /// <param name="botAdapter">Bot adapter.</param>
        /// <param name="teamInfo">DB team model info.</param>
        /// <returns>The name of the team</returns>
        public virtual async Task<string> GetTeamNameByIdAsync(BotAdapter botAdapter, TeamInstallInfo teamInfo)
        {
            TeamDetails teamDetails = null;
            await this.ExecuteInNewTurnContext(botAdapter, teamInfo, async (newTurnContext, newCancellationToken) =>
            {
                teamDetails = await this.GetTeamDetailsAsync(newTurnContext, teamInfo.TeamId, newCancellationToken);
            });
            return teamDetails?.Name;
        }

        /// <summary>
        /// Get team members.
        /// </summary>
        /// <param name="botAdapter">Bot adapter.</param>
        /// <param name="teamInfo">The team that the bot has been installed to</param>
        /// <returns>List of team members channel accounts</returns>
        public virtual async Task<IList<ChannelAccount>> GetTeamMembers(BotAdapter botAdapter, TeamInstallInfo teamInfo)
        {
            var members = new List<ChannelAccount>();
            await this.ExecuteInNewTurnContext(botAdapter, teamInfo, async (turnContext, cancellationToken) =>
            {
                string continuationToken = null;
                do
                {
                    var pagedResult = await TeamsInfo.GetPagedTeamMembersAsync(turnContext, teamInfo.TeamId, continuationToken, pageSize: 500);
                    continuationToken = pagedResult.ContinuationToken;
                    if (pagedResult.Members != null)
                    {
                        members.AddRange(pagedResult.Members);
                    }
                }
                while (continuationToken != null);
            });
            return members;
        }

        /// <summary>
        /// Create a new turn context and execute callback parameter to do desired function
        /// </summary>
        /// <param name="botAdapter">Bot adapter.</param>
        /// <param name="teamInfo">The team that the bot has been installed to</param>
        /// <param name="callback">The method to call for the resulting bot turn.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        private async Task ExecuteInNewTurnContext(BotAdapter botAdapter, TeamInstallInfo teamInfo, BotCallbackHandler callback)
        {
            var conversationReference = new ConversationReference
            {
                ServiceUrl = teamInfo.ServiceUrl,
                Conversation = new ConversationAccount
                {
                    Id = teamInfo.TeamId,
                },
            };

            await botAdapter.ContinueConversationAsync(
                this.appSettings.MicrosoftAppId,
                conversationReference,
                callback,
                default(CancellationToken)).ConfigureAwait(false);
        }
    }
}