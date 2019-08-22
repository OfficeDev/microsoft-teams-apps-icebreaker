// <copyright file="WelcomeTeamCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Cards
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// This class is responsible for generating the welcome team adaptive card.
    /// </summary>
    public class WelcomeTeamCard
    {
        /// <summary>
        /// Builds the adaptive card to welcome the team.
        /// </summary>
        /// <returns>An adaptive card attachment to append to a message.</returns>
        /// <param name="teamName">The team name.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <param name="botInstaller">The name of the person who installed the bot.</param>
        public static Attachment GetCard(
            string teamName,
            string botDisplayName,
            string botInstaller)
        {
            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png";
            AdaptiveCard teamWelcomeCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = Resources.SalutationTitleText,
                        Size = AdaptiveTextSize.Large,
                        Wrap = false,
                    },
                    new AdaptiveImage
                    {
                        Url = new Uri(welcomeCardImageUrl),
                    },
                    new AdaptiveTextBlock
                    {
                        Text = string.IsNullOrEmpty(botInstaller) ?
                        string.Format(Resources.InstallMessageUnknownInstallerPart1, teamName) :
                        string.Format(Resources.InstallMessageKnownInstallerPart1, botInstaller, teamName),
                        Wrap = true,
                    },
                    new AdaptiveTextBlock
                    {
                        Text = string.IsNullOrEmpty(botInstaller) ?
                        Resources.InstallMessageUnknownInstallerPart2 :
                        Resources.InstallMessageKnownInstallerPart2,
                        Wrap = true,
                        Spacing = AdaptiveSpacing.Small,
                    },
                    new AdaptiveTextBlock
                    {
                        Text = string.IsNullOrEmpty(botInstaller) ?
                        Resources.InstallMessageUnknownInstallerPart3 :
                        Resources.InstallMessageKnownInstallerPart3,
                        Wrap = true,
                        Spacing = AdaptiveSpacing.Small,
                    }
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = teamWelcomeCard,
            };
        }
    }
}