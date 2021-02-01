//----------------------------------------------------------------------------------------------
// <copyright file="TeamsViewCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker.Helpers.AdaptiveCards
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using global::AdaptiveCards;
    using Icebreaker.Helpers;
    using Icebreaker.Properties;
    using Microsoft.Azure;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Newtonsoft.Json;

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
                        Text = "Here are your teams! Select the teams that you would like to pause matches for.",
                        Wrap = true
                    },
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "Save",
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

/*        /// <summary>
        /// Method to show teams view card.
        /// </summary>
        /// <param name="userInfo">User info.</param>
        /// <returns>Returns an attachment of personal goal details card in personal scope.</returns>
        public static Attachment GetTeamsViewCard(UserInfo userInfo)
        {
            personalGoalDetails = personalGoalDetails ?? throw new ArgumentNullException(nameof(personalGoalDetails));
            var goalCycleStartDate = personalGoalDetails.Select(goal => goal.StartDate).First();
            var goalCycleEndDate = personalGoalDetails.Select(goal => goal.EndDate).First();

            // Start date and end date are stored in storage with user specific time zones.
            goalCycleStartDate = DateTime.Parse(goalCycleStartDate, CultureInfo.CurrentCulture)
                .ToString(Constants.ListCardDateTimeFormat, CultureInfo.CurrentCulture);
            goalCycleEndDate = DateTime.Parse(goalCycleEndDate, CultureInfo.CurrentCulture)
                .ToString(Constants.ListCardDateTimeFormat, CultureInfo.CurrentCulture);

            var reminder = personalGoalDetails.Select(goal => goal.ReminderFrequency).First();
            var reminderFrequency = (ReminderFrequency)reminder;
            ListCard personalGoalDetailsListCard = new ListCard
            {
                Title = localizer.GetString("PersonalGoalListCardTitle"),
            };

            personalGoalDetailsListCard.Items.Add(new ListItem
            {
                Title = localizer.GetString("PersonalGoalCardCycleText", goalCycleStartDate, goalCycleEndDate),
                Type = "section",
            });

            foreach (var personalGoalDetailEntity in personalGoalDetails)
            {
                personalGoalDetailsListCard.Items.Add(new ListItem
                {
                    Id = personalGoalDetailEntity.PersonalGoalId,
                    Title = personalGoalDetailEntity.GoalName,
                    Subtitle = reminderFrequency.ToString(),
                    Type = "resultItem",
                    Tap = new TaskModuleAction(localizer.GetString("PersonalGoalEditButtonText"),
                    new AdaptiveSubmitAction
                    {
                        Title = localizer.GetString("PersonalGoalEditButtonText"),
                        Data = new AdaptiveSubmitActionData
                        {
                            AdaptiveActionType = Constants.EditPersonalGoalsCommand,
                            GoalCycleId = goalCycleId,
                        },
                    }),
                    Icon = $"{applicationBasePath}/Artifacts/listIcon.png",
                });
            }

            CardAction editButton = new TaskModuleAction(localizer.GetString("PersonalGoalEditButtonText"), new AdaptiveSubmitAction
            {
                Data = new AdaptiveSubmitActionData
                {
                    AdaptiveActionType = Constants.EditPersonalGoalsCommand,
                    GoalCycleId = goalCycleId,
                },
            });
            personalGoalDetailsListCard.Buttons.Add(editButton);

            CardAction addNoteButton = new TaskModuleAction(localizer.GetString("PersonalGoalAddNoteButtonText"), new AdaptiveSubmitAction
            {
                Data = new AdaptiveSubmitActionData
                {
                    AdaptiveActionType = Constants.AddNoteCommand,
                    GoalCycleId = goalCycleId,
                },
            });
            personalGoalDetailsListCard.Buttons.Add(addNoteButton);

            var personalGoalsListCard = new Attachment()
            {
                ContentType = "application/vnd.microsoft.teams.card.list",
                Content = personalGoalDetailsListCard,
            };

            return personalGoalsListCard;
        }*/
    }
}