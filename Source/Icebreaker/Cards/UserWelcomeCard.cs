// <copyright file="UserWelcomeCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Cards
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Icebreaker.Helpers;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// This class is responsible for generating the adaptive card to welcome the user.
    /// </summary>
    public class UserWelcomeCard
    {
        /// <summary>
        /// This method is responsible for constructing the user welcome attachment.
        /// </summary>
        /// <returns>The attachment to append to a message.</returns>
        /// <param name="botInstaller">The user that installed the bot.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        /// <param name="teamName">The name of the team the bot has been installed to.</param>
        public static Attachment GetCard(string botInstaller, string botDisplayName, string teamName)
        {
            var baseDomain = CloudConfigurationManager.GetSetting("AppBaseDomain");
            var appId = CloudConfigurationManager.GetSetting("ManifestAppId");
            var tourTitle = Resources.WelcomeTourTitle;

            var welcomeCardImageUrl = $"https://{baseDomain}/Content/welcome-card-image.png";
            var htmlUrl = Uri.EscapeDataString($"https://{baseDomain}/Content/tour.html?theme={{theme}}");
            var tourUrl = $"https://teams.microsoft.com/l/task/{appId}?url={htmlUrl}&height=533px&width=600px&title={tourTitle}";

            AdaptiveCard userWelcomeCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Size = AdaptiveTextSize.Medium,
                        Weight = AdaptiveTextWeight.Bolder,
                        Text = Resources.SalutationTitleText,
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
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveOpenUrlAction
                    {
                        Title = Resources.TakeATourButtonText,
                        Url = new Uri(tourUrl),
                    },
                    new AdaptiveSubmitAction
                    {
                        Title = Resources.PausePairingsButtonText,
                        Data = new TeamsAdaptiveSubmitActionData
                        {
                            MsTeams = new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                DisplayText = Resources.PausePairingsButtonText,
                                Text = "optout",
                            },
                        },
                    },
                },
            };

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = userWelcomeCard,
            };
        }
    }
}