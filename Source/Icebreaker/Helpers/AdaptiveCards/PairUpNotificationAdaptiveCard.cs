//----------------------------------------------------------------------------------------------
// <copyright file="PairUpNotificationAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;
    using Icebreaker.Properties;
    using Microsoft.Bot.Connector.Teams.Models;

    /// <summary>
    /// Builder class for the pairup notification card
    /// </summary>
    public static class PairUpNotificationAdaptiveCard
    {
        /// <summary>
        /// Default marker string in the UPN that indicates a user is externally-authenticated
        /// </summary>
        private const string ExternallyAuthenticatedUpnMarker = "#ext#";

        private static readonly string CardTemplate;

        static PairUpNotificationAdaptiveCard()
        {
            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/PairUpNotificationAdaptiveCard.json");
            CardTemplate = File.ReadAllText(cardJsonFilePath);
        }

        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <param name="teamName">The team name.</param>
        /// <param name="sender">The user who will be sending this card.</param>
        /// <param name="recipient">The user who will be receiving this card.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <returns>Pairup notification card</returns>
        public static string GetCard(string teamName, TeamsChannelAccount sender, TeamsChannelAccount recipient, string botDisplayName)
        {
            // Guest users may not have their given name specified in AAD, so fall back to the full name if needed
            var senderGivenName = string.IsNullOrEmpty(sender.GivenName) ? sender.Name : sender.GivenName;
            var recipientGivenName = string.IsNullOrEmpty(recipient.GivenName) ? recipient.Name : recipient.GivenName;

            // To start a chat with a guest user, use their external email, not the UPN
            var recipientUpn = !IsGuestUser(recipient) ? recipient.UserPrincipalName : recipient.Email;

            var meetingTitle = string.Format(Resources.MeetupTitle, senderGivenName, recipientGivenName);
            var meetingContent = string.Format(Resources.MeetupContent, botDisplayName);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + Uri.EscapeDataString(meetingTitle) + "&attendees=" + recipientUpn + "&content=" + Uri.EscapeDataString(meetingContent);

            var matchUpCardTitleContent = Resources.MatchUpCardTitleContent;
            var matchUpCardMatchedText = string.Format(Resources.MatchUpCardMatchedText, recipient.Name);
            var matchUpCardContentPart1 = string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, recipient.Name);
            var matchUpCardContentPart2 = Resources.MatchUpCardContentPart2;
            var chatWithMatchButtonText = string.Format(Resources.ChatWithMatchButtonText, recipientGivenName);
            var pauseMatchesButtonText = Resources.PausePairingsButtonText;
            var proposeMeetupButtonText = Resources.ProposeMeetupButtonText;

            var variablesToValues = new Dictionary<string, string>()
            {
                { "matchUpCardTitleContent", matchUpCardTitleContent },
                { "matchUpCardMatchedText", matchUpCardMatchedText },
                { "matchUpCardContentPart1", matchUpCardContentPart1 },
                { "matchUpCardContentPart2", matchUpCardContentPart2 },
                { "chatWithMatchButtonText", chatWithMatchButtonText },
                { "pauseMatchesButtonText", pauseMatchesButtonText },
                { "proposeMeetupButtonText", proposeMeetupButtonText },
                { "meetingLink", meetingLink },
                { "personUpn", recipientUpn }
            };

            var cardBody = CardTemplate;
            foreach (var kvp in variablesToValues)
            {
                cardBody = cardBody.Replace($"%{kvp.Key}%", kvp.Value);
            }

            return cardBody;
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