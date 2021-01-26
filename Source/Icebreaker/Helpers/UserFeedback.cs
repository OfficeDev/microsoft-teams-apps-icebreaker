//----------------------------------------------------------------------------------------------
// <copyright file="UserFeedback.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a user
    /// </summary>
    public class UserFeedback : Document
    {
        /// <summary>
        /// Gets or sets the id of the associated
        /// </summary>
        [JsonProperty("teamId")]
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the user's rating
        /// </summary>
        [JsonProperty("feedbackRating")]
        public string FeedbackRating { get; set; }

        /// <summary>
        /// Gets or sets the text of the comment by the user
        /// </summary>
        [JsonProperty("feedbackText")]
        public string FeedbackText { get; set; }
    }
}