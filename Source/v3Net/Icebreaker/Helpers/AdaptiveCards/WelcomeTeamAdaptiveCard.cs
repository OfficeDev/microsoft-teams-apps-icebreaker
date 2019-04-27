//----------------------------------------------------------------------------------------------
// <copyright file="WelcomeTeamAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------
namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;
    using Icebreaker.Properties;
    using Microsoft.Azure;

    /// <summary>
    /// Builder class for the team welcome message
    /// </summary>
    public class WelcomeTeamAdaptiveCard
    {
        private static readonly string CardTemplate;

        static WelcomeTeamAdaptiveCard()
        {
            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/WelcomeTeamCard.json");
            CardTemplate = File.ReadAllText(cardJsonFilePath);
        }

        /// <summary>
        /// Creates the adaptive card for the team welcome message
        /// </summary>
        /// <param name="teamName">The team name</param>
        /// <param name="botDisplayName">The bot display name</param>
        /// <param name="botInstaller">The name of the person that installed the bot</param>
        /// <returns>The welcome team adaptive card</returns>
        public static string GetCard(string teamName, string botDisplayName, string botInstaller)
        {
            string teamIntroMessage = string.Empty;
            if (string.IsNullOrEmpty(botInstaller))
            {
                teamIntroMessage = string.Format(Resources.InstallMessageUnknownInstaller, teamName);
            }
            else
            {
                teamIntroMessage = string.Format(Resources.InstallMessageKnownInstaller, botInstaller, teamName);
            }

            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var htmlUrl = Uri.EscapeDataString($"https://{baseDomain}/Content/tour.html");
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");

            var tourUrl = $"https://teams.microsoft.com/l/task/{appId}?url={htmlUrl}&height=533px&width=600px&title={tourTitle}";

            var variablesToValues = new Dictionary<string, string>()
            {
                { "intro", teamIntroMessage },
                { "tourUrl", tourUrl }
            };

            var cardBody = CardTemplate;
            foreach (var kvp in variablesToValues)
            {
                cardBody = cardBody.Replace($"%{kvp.Key}%", kvp.Value);
            }

            return cardBody;
        }
    }
}