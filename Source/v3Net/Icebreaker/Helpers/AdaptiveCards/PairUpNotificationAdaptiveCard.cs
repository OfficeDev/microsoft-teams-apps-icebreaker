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
        /// <param name="otherEmailAddress">The email address of the person</param>
        /// <returns>Pairup notification card</returns>
        public static string GetCard(string teamName, string matchedPersonName, string matchedPersonFirstName, string receiverName, string personUpn, string botDisplayName, string otherEmailAddress)
        {
            var title = $"{matchedPersonName} / {matchedPersonFirstName} Meet up";
            var titleEncoding = Uri.EscapeUriString(title);

            var content = $"Hey there! Looks like {botDisplayName} matched us this week. It'd be great to meet up for a coffee or a lunch or a call if you've got time.";
            var contentEncoding = Uri.EscapeUriString(content);
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + titleEncoding + "&attendees=" + otherEmailAddress + "&content=" + contentEncoding;

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