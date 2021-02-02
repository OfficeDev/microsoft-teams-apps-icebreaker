//----------------------------------------------------------------------------------------------
// <copyright file="ListItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//-----

namespace Icebreaker.Helpers.AdaptiveCards
{
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// A class that represents the list item model.
    /// </summary>
    public class ListItem
    {
        /// <summary>
        /// Gets or sets type of item.
        /// </summary>
        [JsonProperty("Type")]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets id of the list card item.
        /// </summary>
        [JsonProperty("Id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets title of list card item.
        /// </summary>
        [JsonProperty("Title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets subtitle of list card item.
        /// </summary>
        [JsonProperty("Subtitle")]
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets tap action for list card item.
        /// </summary>
        [JsonProperty("Tap")]
        public CardAction Tap { get; set; }

        /// <summary>
        /// Gets or sets icon for list card item.
        /// </summary>
        [JsonProperty("Icon")]
        public string Icon { get; set; }
    }
}