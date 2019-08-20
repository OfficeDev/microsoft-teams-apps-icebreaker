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
        /// <param name="firstPerson">The first <see cref="TeamsChannelAccount"/> of the matched pair.</param>
        /// <param name="secondPerson">The second <see cref="TeamsChannelAccount"/> of the matched pair.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <returns>Pairup notification card</returns>
        public static string GetCard(string teamName, TeamsChannelAccount firstPerson, TeamsChannelAccount secondPerson, string botDisplayName)
        {
            var firstPersonFirstName = string.IsNullOrEmpty(firstPerson.GivenName) ? firstPerson.Name : firstPerson.GivenName;
            var secondPersonFirstName = string.IsNullOrEmpty(secondPerson.GivenName) ? secondPerson.Name : secondPerson.GivenName;

            var title = string.Format(Resources.MeetupTitle, firstPersonFirstName, secondPersonFirstName);
            var escapedTitle = Uri.EscapeDataString(title);

            var content = string.Format(Resources.MeetupContent, botDisplayName);
            var escapedContent = Uri.EscapeDataString(content);

            var personUpn = IsGuestUser(secondPerson) ? secondPerson.Email : secondPerson.UserPrincipalName;
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + escapedTitle + "&attendees=" + personUpn + "&content=" + escapedContent;

            var matchUpCardTitleContent = Resources.MatchUpCardTitleContent;
            var matchUpCardMatchedText = string.Format(Resources.MatchUpCardMatchedText, secondPerson.Name);
            var matchUpCardContentPart1 = string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, secondPerson.Name);
            var matchUpCardContentPart2 = Resources.MatchUpCardContentPart2;
            var chatWithMatchButtonText = string.Format(Resources.ChatWithMatchButtonText, secondPersonFirstName);
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
                { "personUpn", personUpn }
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
        /// <returns>A value to indicate if the account is a user.</returns>
        private static bool IsGuestUser(TeamsChannelAccount account)
        {
            return account.UserPrincipalName.IndexOf("#ext#", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}