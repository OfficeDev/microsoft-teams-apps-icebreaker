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

    /// <summary>
    /// Builder class for the pairup notification card
    /// </summary>
    public static class PairUpNotificationAdaptiveCard
    {
        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <param name="teamName">Name of the team</param>
        /// <param name="firstPersonName">Name of the matched person</param>
        /// <param name="secondPersonName">First name of the matched person</param>
        /// <param name="firstPersonFirstName">First name of the first person</param>
        /// <param name="secondPersonFirstName">First name of the second person</param>
        /// <param name="receiverName">Name of the receiver</param>
        /// <param name="personUpn">UPN of the person</param>
        /// <param name="botDisplayName">This is the display name of the bot that is set from the deployment</param>
        /// <returns>Pairup notification card</returns>
        public static string GetCard(string teamName, string firstPersonName, string secondPersonName, string firstPersonFirstName, string secondPersonFirstName, string receiverName, string personUpn, string botDisplayName)
        {
            var title = string.Format(Resources.MeetupTitle, firstPersonFirstName, secondPersonFirstName);
            var escapedTitle = Uri.EscapeDataString(title);

            var content = string.Format(Resources.MeetupContent, botDisplayName);
            var escapedContent = Uri.EscapeDataString(content);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + escapedTitle + "&attendees=" + personUpn + "&content=" + escapedContent;

            var variablesToValues = new Dictionary<string, string>()
            {
                { "team", teamName },
                { "matchedPerson", firstPersonName },
                { "matchedPersonFirstName", secondPersonName },
                { "receiverName", firstPersonFirstName },
                { "personUpn", personUpn },
                { "botDisplayName", botDisplayName },
                { "meetingLink", meetingLink },
                { "pauseMatches", Resources.PausePairingsButtonText }
            };

            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/PairUpNotificationAdaptiveCard.json");
            var cardTemplate = File.ReadAllText(cardJsonFilePath);

            var cardBody = cardTemplate;

            foreach (var kvp in variablesToValues)
            {
                cardBody = cardBody.Replace($"%{kvp.Key}%", kvp.Value);
            }

            return cardBody;
        }
    }
}