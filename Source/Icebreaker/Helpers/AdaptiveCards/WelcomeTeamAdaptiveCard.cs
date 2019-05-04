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
            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/WelcomeTeamAdaptiveCard.json");
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
            string teamIntroPart1 = string.Empty;
            string teamIntroPart2 = string.Empty;
            string teamIntroPart3 = string.Empty;

            if (string.IsNullOrEmpty(botInstaller))
            {
                teamIntroPart1 = string.Format(Resources.InstallMessageUnknownInstallerPart1, teamName);
                teamIntroPart2 = Resources.InstallMessageUnknownInstallerPart2;
                teamIntroPart3 = Resources.InstallMessageUnknownInstallerPart3;
            }
            else
            {
                teamIntroPart1 = string.Format(Resources.InstallMessageKnownInstallerPart1, botInstaller, teamName);
                teamIntroPart2 = Resources.InstallMessageKnownInstallerPart2;
                teamIntroPart3 = Resources.InstallMessageKnownInstallerPart3;
            }

            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var htmlUrl = Uri.EscapeDataString($"https://{baseDomain}/Content/tour.html?theme={{theme}}");
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");
            var welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png";
            var tourUrl = $"https://teams.microsoft.com/l/task/{appId}?url={htmlUrl}&height=533px&width=600px&title={tourTitle}";
            var salutationText = Resources.SalutationTitleText;
            var tourButtonText = Resources.TakeATourButtonText;

            var variablesToValues = new Dictionary<string, string>()
            {
                { "teamIntroPart1", teamIntroPart1 },
                { "teamIntroPart2", teamIntroPart2 },
                { "teamIntroPart3", teamIntroPart3 },
                { "welcomeCardImageUrl", welcomeCardImageUrl },
                { "tourUrl", tourUrl },
                { "salutationText", salutationText },
                { "tourButtonText", tourButtonText }
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