//----------------------------------------------------------------------------------------------
// <copyright file="PairUpNotificationAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

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
        /// <param name="recipient">The user who will be receiving this card.</param>
        /// <param name="sender">The user who will be sending this card.</param>
        /// <param name="senderProfile">The profile of the sender.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <returns>Pairup notification card</returns>
        public static Attachment GetCard(string teamName, TeamsChannelAccount recipient, TeamsChannelAccount sender, string senderProfile, string botDisplayName)
        {
            // Guest users may not have their given name specified in AAD, so fall back to the full name if needed
            var recipientGivenName = string.IsNullOrEmpty(recipient.GivenName) ? recipient.Name : recipient.GivenName;
            var senderGivenName = string.IsNullOrEmpty(sender.GivenName) ? sender.Name : sender.GivenName;

            // To start a chat with a guest user, use their external email, not the UPN
            var senderUpn = !IsGuestUser(sender) ? sender.UserPrincipalName : sender.Email;

            var meetingTitle = string.Format(Resources.MeetupTitle, recipientGivenName, senderGivenName);
            var meetingContent = string.Format(Resources.MeetupContent, botDisplayName);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + Uri.EscapeDataString(meetingTitle) + "&attendees=" + senderUpn + "&content=" + Uri.EscapeDataString(meetingContent);

            var cardData = new
            {
                matchUpCardTitleContent = Resources.MatchUpCardTitleContent,
                matchUpCardMatchedText = string.Format(Resources.MatchUpCardMatchedText, sender.Name),
                matchUpCardContentPart1 = string.IsNullOrEmpty(senderProfile) ?
                    string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, sender.Name) :
                    string.Format(Resources.MatchUpCardContentPart1b, botDisplayName, teamName, sender.Name, senderProfile),
                matchUpCardContentPart2 = Resources.MatchUpCardContentPart2,
                chatWithMatchButtonText = string.Format(Resources.ChatWithMatchButtonText, senderGivenName),
                chatWithMessageGreeting = Resources.ChatWithMessageGreeting,
                pauseMatchesButtonText = Resources.PausePairingsButtonText,
                proposeMeetupButtonText = Resources.ProposeMeetupButtonText,
                personUpn = senderUpn,
                viewTeamsButton = Resources.ViewTeamsButtonText,
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