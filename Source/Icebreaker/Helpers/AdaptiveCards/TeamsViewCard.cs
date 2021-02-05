//----------------------------------------------------------------------------------------------
// <copyright file="TeamsViewCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System.Collections.Generic;
    using System.Linq;
    using global::AdaptiveCards;
    using Icebreaker.Helpers;
    using Icebreaker.Properties;
    using Microsoft.Bot.Schema;

    /// <summary>
    /// Class for teams view card
    /// </summary>
    public class TeamsViewCard
    {
        /// <summary>
        /// Gets the teams view card
        /// <param name="userInfo">User info.</param>
        /// <param name="teamNameLookup">Team id to name</param>
        /// </summary>
        /// <returns>Returns an attachment of teams view card.</returns>
        public static Attachment GetTeamsViewCard(UserInfo userInfo, Dictionary<string, string> teamNameLookup)
        {
            AdaptiveCard teamsViewCard = new AdaptiveCard("1.2")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        HorizontalAlignment = AdaptiveHorizontalAlignment.Left,
                        Text = Resources.ViewTeamsText,
                        Wrap = true
                    },
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = Resources.SaveButtonText,
                        Data = new
                        {
                            ActionType = "saveopt"
                        },
                    },
                },
            };

            var optedIn = userInfo.OptedIn;
            foreach (var teamId in optedIn.Keys.ToList())
            {
                teamsViewCard.Body.Add(new AdaptiveToggleInput
                {
                    Title = teamNameLookup[teamId],
                    Id = teamId,
                    Value = optedIn[teamId].ToString().ToLower(),
                    ValueOff = "true",
                    ValueOn = "false"
                });
            }

            return new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = teamsViewCard,
            };
        }
    }
}