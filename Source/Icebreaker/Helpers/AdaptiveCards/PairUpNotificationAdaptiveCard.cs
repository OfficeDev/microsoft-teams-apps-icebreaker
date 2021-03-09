// <copyright file="PairUpNotificationAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using global::AdaptiveCards.Templating;
    using Icebreaker.Properties;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Builder class for the pairup notification card
    /// </summary>
    public class PairUpNotificationAdaptiveCard : AdaptiveCardBase
    {
        /// <summary>
        /// Default marker string in the UPN that indicates a user is externally-authenticated
        /// </summary>
        private const string ExternallyAuthenticatedUpnMarker = "#ext#";

        private static readonly Lazy<AdaptiveCardTemplate> AdaptiveCardTemplate =
            new Lazy<AdaptiveCardTemplate>(() => CardTemplateHelper.GetAdaptiveCardTemplate(AdaptiveCardName.PairUpNotification));

        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <param name="teamName">The team name.</param>
        /// <param name="sender">The user who will be sending this card.</param>
        /// <param name="recipient">The user who will be receiving this card.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <returns>Pairup notification card</returns>
        public static Attachment GetCard(string teamName, TeamsChannelAccount sender, TeamsChannelAccount recipient, string botDisplayName)
        {
            // Guest users may not have their given name specified in AAD, so fall back to the full name if needed
            var senderGivenName = string.IsNullOrEmpty(sender.GivenName) ? sender.Name : sender.GivenName;
            var recipientGivenName = string.IsNullOrEmpty(recipient.GivenName) ? recipient.Name : recipient.GivenName;

            // To start a chat with a guest user, use their external email, not the UPN
            var recipientUpn = !IsGuestUser(recipient) ? recipient.UserPrincipalName : recipient.Email;

            var meetingTitle = string.Format(Resources.MeetupTitle, senderGivenName, recipientGivenName);
            var meetingContent = string.Format(Resources.MeetupContent, botDisplayName);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + Uri.EscapeDataString(meetingTitle) + "&attendees=" + recipientUpn + "&content=" + Uri.EscapeDataString(meetingContent);

            var cardData = new
            {
                matchUpCardTitleContent = Resources.MatchUpCardTitleContent,
                matchUpCardMatchedText = string.Format(Resources.MatchUpCardMatchedText, recipient.Name),
                matchUpCardContentPart1 = string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, recipient.Name),
                matchUpCardContentPart2 = Resources.MatchUpCardContentPart2,
                chatWithMatchButtonText = string.Format(Resources.ChatWithMatchButtonText, recipientGivenName),
                chatWithMessageGreeting = Resources.ChatWithMessageGreeting,
                pauseMatchesButtonText = Resources.PausePairingsButtonText,
                proposeMeetupButtonText = Resources.ProposeMeetupButtonText,
                personUpn = recipientUpn,
                meetingLink,
            };

            return GetCard(AdaptiveCardTemplate.Value, cardData);
        }

        /// <summary>
        /// Checks whether or not the account is a guest user.
        /// </summary>
        /// <param name="account">The <see cref="TeamsChannelAccount"/> user to check.</param>
        /// <returns>True if the account is a guest user, false otherwise.</returns>
        private static bool IsGuestUser(TeamsChannelAccount account)
        {
            return account.UserPrincipalName.IndexOf(ExternallyAuthenticatedUpnMarker, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}