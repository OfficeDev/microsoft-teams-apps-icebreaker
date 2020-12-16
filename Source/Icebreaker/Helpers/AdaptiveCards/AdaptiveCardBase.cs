// <copyright file="AdaptiveCardBase.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using global::AdaptiveCards;
    using global::AdaptiveCards.Templating;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Builder class for the adaptive cards
    /// </summary>
    public class AdaptiveCardBase
    {
        /// <summary>
        /// Creates the adaptive card from by processing template and related data
        /// </summary>
        /// <param name="template">Adaptive template</param>
        /// <param name="cardData">card data to merge into template</param>
        /// <returns>Card attachment</returns>
        protected static Attachment GetCard(AdaptiveCardTemplate template, dynamic cardData)
        {
            // "Expand" the template - this generates the final Adaptive Card payload
            var cardJson = template.Expand(cardData);

            try
            {
                var welcomeCard = AdaptiveCard.FromJson(cardJson);
                return new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = welcomeCard.Card,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}