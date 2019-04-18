//----------------------------------------------------------------------------------------------
// <copyright file="TeamInstallInfo.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers
{
    using Microsoft.Azure.Documents;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents information about a team to which the Icebreaker app was installed
    /// </summary>
    public class TeamInstallInfo : Document
    {
        /// <summary>
        /// Gets or sets the team id.
        /// This is also the <see cref="Resource.Id"/>.
        /// </summary>
        [JsonIgnore]
        public string TeamId
        {
            get { return this.Id; }
            set { this.Id = value; }
        }

        /// <summary>
        /// Gets or sets the tenant id
        /// </summary>
        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the service URL
        /// </summary>
        [JsonProperty("serviceUrl")]
        public string ServiceUrl { get; set; }

        /// <summary>
        /// Gets or sets the name of the person that installed the bot to the team
        /// </summary>
        [JsonProperty("installerName")]
        public string InstallerName { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Team - Id = {this.TeamId}, TenantId = {this.TenantId}, ServiceUrl = {this.ServiceUrl}, Installer = {this.InstallerName}";
        }
    }
}