// <copyright file="FeedbackAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Web.Hosting;
    using Properties;

    /// <summary>
    /// Builder class for the pairup notification card
    /// </summary>
    public static class FeedbackAdaptiveCard
    {
        private static readonly string CardTemplate;

        static FeedbackAdaptiveCard()
        {
            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/FeedbackAdaptiveCard.json");
            CardTemplate = File.ReadAllText(cardJsonFilePath);
        }

        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <param name="firstPersonName">Name of the matched person</param>
        /// <param name="firstPersonFirstName">First name of the first person</param>
        /// <param name="personUpn1">UPN of the First person</param>
        /// <param name="personUpn2">UPN of the Second person</param>
        /// <param name="feedbackId">Feedback card id</param>
        /// <returns>Pair up notification card</returns>
        public static string GetCard(string firstPersonName, string firstPersonFirstName, string personUpn1, string personUpn2, string feedbackId)
        {
            var feedbackCardTitleContent = string.Format(Resources.FeedbackWelcomeText, firstPersonName);
            var feedbackPersonGivenTo = firstPersonName;
            var feedbackGivenFrom = personUpn2;
            var feedbackGivenTo = personUpn1;
            var upload = "Upload%20meetup%20photo";
            var chara = "-";
            var url = "https://icebreaker-y5l22i4ytireq.azurewebsites.net/Upload/Upload?UserDetails=" + feedbackId + chara + feedbackGivenFrom + chara + feedbackGivenTo;
            var appId = ConfigurationManager.AppSettings["ManifestAppId"];
            var microsoftAppId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var uploadLink = "https://teams.microsoft.com/l/task/" + appId + "?url=" + url + "&height=200&width=400&title=" + upload + "&completionBotId=" + microsoftAppId;
            var variablesToValues = new Dictionary<string, string>()
            {
                { "feedbackCardTitleContent", feedbackCardTitleContent },
                { "feedbackPersonGivenTo", feedbackPersonGivenTo },
                { "feedbackGivenFrom", feedbackGivenFrom },
                { "feedbackGivenTo", feedbackGivenTo },
                { "feedbackId", feedbackId },
                { "uploadLink", uploadLink }
            };

            var cardBody = CardTemplate;
            foreach (var kvp in variablesToValues)
            {
                cardBody = cardBody.Replace($"%{kvp.Key}%", kvp.Value);
            }

            return cardBody;
        }
    }
}