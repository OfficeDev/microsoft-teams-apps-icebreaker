//----------------------------------------------------------------------------------------------
// <copyright file="WelcomeNewMemberAdaptiveCard.cs" company="Microsoft">
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
    /// Builder class for the welcome new member card
    /// </summary>
    public static class WelcomeNewMemberAdaptiveCard
    {
        private static readonly string CardTemplate;

        static WelcomeNewMemberAdaptiveCard()
        {
            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/WelcomeNewMemberAdaptiveCard.json");
            CardTemplate = File.ReadAllText(cardJsonFilePath);
        }

        /// <summary>
        /// Creates the welcome new member card.
        /// </summary>
        /// <param name="teamName">The team name</param>
        /// <param name="personFirstName">The first name of the new member</param>
        /// <param name="botDisplayName">The bot name</param>
        /// <param name="botInstaller">The person that installed the bot to the team</param>
        /// <returns>The welcome new member card</returns>
        public static string GetCard(string teamName, string personFirstName, string botDisplayName, string botInstaller)
        {
            string introMessagePart1 = string.Empty;
            string introMessagePart2 = string.Empty;
            string introMessagePart3 = string.Empty;

            if (string.IsNullOrEmpty(botInstaller))
            {
                introMessagePart1 = string.Format(Resources.InstallMessageUnknownInstallerPart1, teamName);
                introMessagePart2 = Resources.InstallMessageUnknownInstallerPart2;
                introMessagePart3 = Resources.InstallMessageUnknownInstallerPart3;
            }
            else
            {
                introMessagePart1 = string.Format(Resources.InstallMessageKnownInstallerPart1, botInstaller, teamName);
                introMessagePart2 = Resources.InstallMessageKnownInstallerPart2;
                introMessagePart3 = Resources.InstallMessageKnownInstallerPart3;
            }

            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var htmlUrl = Uri.EscapeDataString($"https://{baseDomain}/Content/tour.html?theme={{theme}}");
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");
            var pauseMatchesText = Resources.PausePairingsButtonText;
            var welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png";
            var tourUrl = $"https://teams.microsoft.com/l/task/{appId}?url={htmlUrl}&height=533px&width=600px&title={tourTitle}";
            var salutationText = Resources.SalutationTitleText;
            var tourButtonText = Resources.TakeATourButtonText;

            var variablesToValues = new Dictionary<string, string>()
            {
                { "team", teamName },
                { "personFirstName", personFirstName },
                { "botDisplayName", botDisplayName },
                { "introMessagePart1", introMessagePart1 },
                { "introMessagePart2", introMessagePart2 },
                { "introMessagePart3", introMessagePart3 },
                { "welcomeCardImageUrl", welcomeCardImageUrl },
                { "pauseMatchesText", pauseMatchesText },
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