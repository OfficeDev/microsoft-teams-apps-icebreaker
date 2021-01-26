//----------------------------------------------------------------------------------------------
// <copyright file="FeedbackAdaptiveCard.cs" company="Microsoft">
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
    public class FeedbackAdaptiveCard : AdaptiveCardBase
    {
        private static readonly Lazy<AdaptiveCardTemplate> AdaptiveCardTemplate =
            new Lazy<AdaptiveCardTemplate>(() => CardTemplateHelper.GetAdaptiveCardTemplate(AdaptiveCardName.Feedback));

        /// <summary>
        /// Creates the pairup notification card.
        /// </summary>
        /// <returns>Pairup notification card</returns>
        public static Attachment GetCard()
        {
            // CLEAN UP HARD CODED TEXT IN JSON
            var cardData = new
            {
                messageContent = Resources.UnrecognizedInput,
                tourButtonText = Resources.TakeATourButtonText
            };

            return GetCard(AdaptiveCardTemplate.Value, cardData);
        }
    }
}