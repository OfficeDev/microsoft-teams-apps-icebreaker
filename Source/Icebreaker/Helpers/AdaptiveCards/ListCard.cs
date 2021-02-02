//----------------------------------------------------------------------------------------------
// <copyright file="ListCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System.Collections.Generic;
    using global::AdaptiveCards;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// A class that represents list card model.
    /// </summary>
    public class ListCard
    {
        /// <summary>
        /// Gets or sets title of goal list card for team and personal bot.
        /// </summary>
        [JsonProperty("Title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets goal list card items to display goal name and reminder frequency.
        /// </summary>
        [JsonProperty("Items")]
        public List<ListItem> Items { get; } = new List<ListItem>();

        /// <summary>
        /// Gets buttons for the goal list card.
        /// </summary>
        [JsonProperty("Buttons")]
        public List<AdaptiveAction> Buttons { get; } = new List<AdaptiveAction>();
    }
}