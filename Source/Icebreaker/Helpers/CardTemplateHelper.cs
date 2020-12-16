// <copyright file="CardTemplateHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers
{
    using System.IO;
    using System.Web.Hosting;
    using global::AdaptiveCards.Templating;

    /// <summary>
    /// Utility functions for constructing cards from templates.
    /// </summary>
    public class CardTemplateHelper
    {
        private const string CardsPath = "~/Helpers/AdaptiveCards";

        /// <summary>
        /// This method will create an instance of adaptiveCardTemplate with the cardPath.
        /// </summary>
        /// <param name="cardPath">cardPath specifies card schema to create Template.</param>
        /// <returns>Adaptive card template.</returns>
        public static AdaptiveCardTemplate GetAdaptiveCardTemplate(string cardPath)
        {
            return new AdaptiveCardTemplate(File.ReadAllText(cardPath));
        }

        /// <summary>
        /// This method will create an instance of adaptiveCardTemplate from a card enum member.
        /// </summary>
        /// <param name="card">Specifies card name to create Template.</param>
        /// <returns>Adaptive card template.</returns>
        public static AdaptiveCardTemplate GetAdaptiveCardTemplate(AdaptiveCardName card)
        {
            return GetAdaptiveCardTemplate(HostingEnvironment.MapPath(Path.Combine(CardsPath, $"{card}AdaptiveCard.json")));
        }
    }
}