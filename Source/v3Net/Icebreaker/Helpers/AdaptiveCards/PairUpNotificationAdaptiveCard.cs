//----------------------------------------------------------------------------------------------
// <copyright file="PairUpNotificationAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers.AdaptiveCards
{
    using Icebreaker.Properties;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;

    /// <summary>
    /// Builder class for the pairup notification card
    /// </summary>
    public static class PairUpNotificationAdaptiveCard
    {
        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <param name="teamName">Name of the team</param>
        /// <param name="matchedPersonName">Name of the matched person</param>
        /// <param name="matchedPersonFirstName">First name of the matched person</param>
        /// <param name="receiverName">Name of the receiver</param>
        /// <param name="personUpn">UPN of the person</param>
        /// <param name="botDisplayName">This is the display name of the bot that is set from the deployment</param>
        /// <returns>Pairup notification card</returns>
        public static string GetCard(string teamName, string matchedPersonName, string matchedPersonFirstName, string receiverName, string personUpn, string botDisplayName)
        {
            var title = string.Format(Resources.MeetupTitle, matchedPersonName, matchedPersonFirstName);
            var titleEncoding = Uri.EscapeDataString(title);

            var content = string.Format(Resources.MeetupContent, botDisplayName);
            var contentEncoding = Uri.EscapeDataString(content);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + titleEncoding + "&attendees=" + personUpn + "&content=" + contentEncoding;

            var variablesToValues = new Dictionary<string, string>()
            {
                { "team", teamName },
                { "matchedPerson", matchedPersonName },
                { "matchedPersonFirstName", matchedPersonFirstName },
                { "receiverName", receiverName },
                { "personUpn", personUpn },
                { "botDisplayName", botDisplayName },
                { "meetingLink", meetingLink }
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