//----------------------------------------------------------------------------------------------
// <copyright file="PairUpNotificationAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;

    public static class PairUpNotificationAdaptiveCard
    {
        public static string GetCard(string teamName, string matchedPersonName, string matchedPersonFirstName, string receiverName, string personUpn)
        {
            var variablesToValues = new Dictionary<string, string>()
            {
                { "team", teamName },
                { "matchedPerson", matchedPersonName },
                { "matchedPersonFirstName", matchedPersonFirstName},
                { "receiverName", receiverName },
                { "personUpn", personUpn }
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