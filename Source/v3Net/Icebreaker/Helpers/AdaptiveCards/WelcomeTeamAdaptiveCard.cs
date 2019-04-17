﻿//----------------------------------------------------------------------------------------------
// <copyright file="WelcomeTeamAdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------
namespace Icebreaker.Helpers.AdaptiveCards
{
    using System.Collections.Generic;
    using System.IO;
    using System.Web.Hosting;
    using Icebreaker.Properties;

    /// <summary>
    /// Builder class for the team welcome message
    /// </summary>
    public class WelcomeTeamAdaptiveCard
    {
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

            var variablesToValues = new Dictionary<string, string>()
            {
                { "intro", teamIntroMessage }
            };

            var cardJsonFilePath = HostingEnvironment.MapPath("~/Helpers/AdaptiveCards/WelcomeTeamCard.json");
            var cardTemplate = File.ReadAllText(cardJsonFilePath);

            var cardBody = cardTemplate;

            foreach (var kvp in variablesToValues)
            {
                cardBody = cardBody.Replace($"%{kvp.Key}%", kvp.Value);
            }

            return cardBody;
        }
    }
}