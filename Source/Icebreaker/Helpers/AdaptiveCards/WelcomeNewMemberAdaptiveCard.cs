// <copyright file="WelcomeNewMemberAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using global::AdaptiveCards.Templating;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Builder class for the welcome new member card
    /// </summary>
    public class WelcomeNewMemberAdaptiveCard : AdaptiveCardBase
    {
        private static readonly Lazy<AdaptiveCardTemplate> AdaptiveCardTemplate =
            new Lazy<AdaptiveCardTemplate>(() => CardTemplateHelper.GetAdaptiveCardTemplate(AdaptiveCardName.WelcomeNewMember));

        /// <summary>
        /// Creates the welcome new member card.
        /// </summary>
        /// <param name="teamName">The team name</param>
        /// <param name="personFirstName">The first name of the new member</param>
        /// <param name="botDisplayName">The bot name</param>
        /// <param name="botInstaller">The person that installed the bot to the team</param>
        /// <returns>The welcome new member card</returns>
        public static Attachment GetCard(string teamName, string personFirstName, string botDisplayName, string botInstaller)
        {
            string introMessagePart1;
            string introMessagePart2;
            string introMessagePart3;

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
            var tourTitle = Resources.WelcomeTourTitle;
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");

            var welcomeData = new
            {
                personFirstName,
                botDisplayName,
                introMessagePart1,
                introMessagePart2,
                introMessagePart3,
                team = teamName,
                welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png",
                pauseMatchesText = Resources.PausePairingsButtonText,
                tourUrl = GetTourFullUrl(appId, GetTourUrl(baseDomain), tourTitle),
                salutationText = Resources.SalutationTitleText,
                tourButtonText = Resources.TakeATourButtonText,
            };

            return GetCard(AdaptiveCardTemplate.Value, welcomeData);
        }
    }
}