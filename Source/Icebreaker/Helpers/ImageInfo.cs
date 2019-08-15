// <copyright file="ImageInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents Uploaded image
    /// </summary>
    public class ImageInfo : Document
    {
        /// <summary>
        /// Gets or sets the user's id in Teams (29:xxx).
        /// This is also the <see cref="Resource.Id"/>.
        /// </summary>
        [JsonIgnore]
        public string ImageId
        {
            get { return this.Id; }
            set { this.Id = value; }
        }

        /// <summary>
        /// Gets or sets the ImageUrl
        /// </summary>
        [JsonProperty("imageurl")]
        public string Imageurl { get; set; }

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

        /// <summary>
        /// Returns properties with values
        /// </summary>
        /// <returns>String</returns>
        public override string ToString()
        {
            return $"Image - Id = {this.ImageId}, ImageUrl = {this.Imageurl},  personGivenFrom= {this.PersonGivenFrom}, personGivenTo = {this.PersonGivenTo} ";
        }
    }
}