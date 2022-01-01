namespace Icebreaker.Helpers
{
    using System.Collections.Generic;
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a user
    /// </summary>
    public class Question : Document
    {
        /// <summary>
        /// Gets or sets the Language
        /// This is also the <see cref="Resource.Id"/>.
        /// </summary>
        [JsonIgnore]
        public string Language
        {
            get { return this.Id; }
            set { this.Id = value; }
        }

        /// <summary>
        /// Gets or sets a Set of Questions
        /// </summary>
        [JsonProperty("questions")]
        public string[] Questions { get; set; }
    }
}