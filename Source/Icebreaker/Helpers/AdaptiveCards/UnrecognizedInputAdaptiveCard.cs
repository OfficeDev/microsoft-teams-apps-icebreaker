// <copyright file="UnrecognizedInputAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using global::AdaptiveCards.Templating;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Builder class for the unrecognized input message
    /// </summary>
    public class UnrecognizedInputAdaptiveCard : AdaptiveCardBase
    {
        private static readonly Lazy<AdaptiveCardTemplate> AdaptiveCardTemplate =
            new Lazy<AdaptiveCardTemplate>(() => CardTemplateHelper.GetAdaptiveCardTemplate(AdaptiveCardName.UnrecognizedInput));

        /// <summary>
        /// Generates the adaptive card string for the unrecognized input.
        /// </summary>
        /// <returns>The adaptive card for the unrecognized input</returns>
        public static Attachment GetCard()
        {
            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");

            var cardData = new
            {
                messageContent = Resources.UnrecognizedInput,
                tourUrl = GetTourFullUrl(appId, GetTourUrl(baseDomain), tourTitle),
                tourButtonText = Resources.TakeATourButtonText,
            };

            return GetCard(AdaptiveCardTemplate.Value, cardData);
        }
    }
}