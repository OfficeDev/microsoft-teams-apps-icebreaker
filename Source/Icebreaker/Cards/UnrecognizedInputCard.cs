// <copyright file="UnrecognizedInputCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Cards
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// This class is responsible for creating the unrecognized input card.
    /// </summary>
    public class UnrecognizedInputCard
    {
        /// <summary>
        /// Method to render/construct the card in the unrecognized input scenario.
        /// </summary>
        /// <returns>An attachment to append to a message.</returns>
        public static Attachment GetCard()
        {
            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");

            var htmlUrl = Uri.EscapeDataString($"https://{baseDomain}/Content/tour.html?theme={{theme}}");
            var escapedTourTitle = Uri.EscapeDataString(tourTitle);
            var escapedTourUrl = Uri.EscapeDataString($"https://teams.microsoft.com/l/task/{appId}?url={htmlUrl}&height=533px&width=600px&title={escapedTourTitle}");

            AdaptiveCard unrecognizedInputCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = Resources.UnrecognizedInput,
                        Wrap = true,
                    },
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveOpenUrlAction
                    {
                        Title = Resources.TakeATourButtonText,
                        Url = new Uri(escapedTourUrl),
                    },
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = unrecognizedInputCard,
            };
        }
    }
}