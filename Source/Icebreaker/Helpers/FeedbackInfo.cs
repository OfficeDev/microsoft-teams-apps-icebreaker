// <copyright file="FeedbackInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a feedback
    /// </summary>
    public class FeedbackInfo : Document
    {
        /// <summary>
        /// Gets or sets the user's id in Teams (29:xxx).
        /// This is also the <see cref="Resource.Id"/>.
        /// </summary>
        [JsonProperty("feedbackId")]
        public string FeedbackId
        {
            get { return this.Id; }
            set { this.Id = value; }
        }

        /// <summary>
        /// Gets or sets the meeting rate
        /// </summary>
        [JsonProperty("meetingRate")]
        public string MeetingRate { get; set; }

        /// <summary>
        /// Gets or sets the sports
        /// </summary>
        [JsonProperty("sports")]
        public string Sports { get; set; }

        /// <summary>
        /// Gets or sets the technology
        /// </summary>
        [JsonProperty("technology")]
        public string Technology { get; set; }

        /// <summary>
        /// Gets or sets the politics
        /// </summary>
        [JsonProperty("politics")]
        public string Politics { get; set; }

        /// <summary>
        /// Gets or sets the reading books
        /// </summary>
        [JsonProperty("readingBooks")]
        public string ReadingBooks { get; set; }

        /// <summary>
        /// Gets or sets the travelling
        /// </summary>
        [JsonProperty("travelling")]
        public string Travelling { get; set; }

        /// <summary>
        /// Gets or sets the entertainment
        /// </summary>
        [JsonProperty("entertainment")]
        public string Entertainment { get; set; }

        /// <summary>
        /// Gets or sets the person given from
        /// </summary>
        [JsonProperty("personGivenFrom")]
        public string PersonGivenFrom { get; set; }

        /// <summary>
        /// Gets or sets the person given to
        /// </summary>
        [JsonProperty("personGivenTo")]
        public string PersonGivenTo { get; set; }
    }
}