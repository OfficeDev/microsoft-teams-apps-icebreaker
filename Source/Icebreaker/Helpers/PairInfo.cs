//----------------------------------------------------------------------------------------------
// <copyright file="PairInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a pairing that occurred
    /// </summary>
    public class PairInfo : Document
    {
        /// <summary>
        /// Gets or sets the ID of the first user of the match
        /// </summary>
        [JsonProperty("user1Id")]
        public string User1Id { get; set; }

        /// <summary>
        /// Gets or sets the ID of the second user of the match
        /// </summary>
        [JsonProperty("user2Id")]
        public string User2Id { get; set; }

        /// <summary>
        /// Gets or sets the match iteration cycle that this match occured in
        /// </summary>
        [JsonProperty("iteration")]
        public int Iteration { get; set; }
    }
}