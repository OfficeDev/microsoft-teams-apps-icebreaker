// <copyright file="WelcomeTeamAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using System.Globalization;
    using global::AdaptiveCards;
    using global::AdaptiveCards.Templating;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Builder class for the team welcome message
    /// </summary>
    public class WelcomeTeamAdaptiveCard : AdaptiveCardBase
    {
        private static readonly Lazy<AdaptiveCardTemplate> AdaptiveCardTemplate =
            new Lazy<AdaptiveCardTemplate>(() => CardTemplateHelper.GetAdaptiveCardTemplate(AdaptiveCardName.WelcomeTeam));

        /// <summary>
        /// Creates the adaptive card for the team welcome message
        /// </summary>
        /// <param name="teamName">The team name</param>
        /// <param name="botInstaller">The name of the person that installed the bot</param>
        /// <returns>The welcome team adaptive card</returns>
        public static Attachment GetCard(string teamName, string botInstaller)
        {
            // Set alignment of text based on default locale.
            var textAlignment = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? AdaptiveHorizontalAlignment.Right.ToString() : AdaptiveHorizontalAlignment.Left.ToString();

            string teamIntroPart1;
            string teamIntroPart2;
            string teamIntroPart3;

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
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");

            var welcomeData = new
            {
                textAlignment,
                teamIntroPart1,
                teamIntroPart2,
                teamIntroPart3,
                welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png",
                tourUrl = GetTourFullUrl(appId, GetTourUrl(baseDomain), tourTitle),
                salutationText = Resources.SalutationTitleText,
                tourButtonText = Resources.TakeATourButtonText,
            };

            return GetCard(AdaptiveCardTemplate.Value, welcomeData);
        }
    }
}