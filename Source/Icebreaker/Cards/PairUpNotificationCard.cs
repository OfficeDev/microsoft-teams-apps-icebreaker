// <copyright file="PairUpNotificationCard.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Cards
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Icebreaker.Helpers;
    using Icebreaker.Properties;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// This class is responsible for creating the pairup notification card.
    /// </summary>
    public class PairUpNotificationCard
    {
        /// <summary>
        /// Method that will return the attachment for the pairup notifications.
        /// </summary>
        /// <returns>An attachment to append to a message.</returns>
        /// <param name="teamName">The team name.</param>
        /// <param name="firstPerson">The first person in the pair up.</param>
        /// <param name="secondPerson">The second person in the pair up.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        public static Attachment GetCard(
            string teamName,
            ChannelAccount firstPerson,
            ChannelAccount secondPerson,
            string botDisplayName)
        {
            var person1 = firstPerson.Properties.ToObject<TeamsChannelAccount>();
            var person2 = secondPerson.Properties.ToObject<TeamsChannelAccount>();

            var firstPersonFirstName = string.IsNullOrEmpty(person1.GivenName) ? firstPerson.Name : person1.GivenName;
            var secondPersonFirstName = string.IsNullOrEmpty(person2.GivenName) ? secondPerson.Name : person2.GivenName;
            var title = string.Format(Resources.MeetupTitle, firstPersonFirstName, secondPersonFirstName);

            var escapedTitle = Uri.EscapeDataString(title);
            var content = string.Format(Resources.MeetupContent, botDisplayName);
            var escapedContent = Uri.EscapeDataString(content);

            var personUpn = IsGuestUser(person2) ? person2.Email : person2.UserPrincipalName;
            var meetingLink = "https://teams.microsoft.com/l/meeting/new?subject=" + escapedTitle + "&attendees=" + personUpn + "&content=" + escapedContent;
            var chatMessageLink = $"https://teams.microsoft.com/l/chat/0/0?users={personUpn}&message=Hi%20there%20";

            AdaptiveCard pairUpCard = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Size = AdaptiveTextSize.Medium,
                        Weight = AdaptiveTextWeight.Bolder,
                        Wrap = true,
                        MaxLines = 2,
                        Text = Resources.MatchUpCardTitleContent,
                    },
                    new AdaptiveTextBlock
                    {
                        Wrap = true,
                        Text = string.Format(Resources.MatchUpCardMatchedText, secondPerson.Name),
                    },
                    new AdaptiveTextBlock
                    {
                        Wrap = true,
                        Text = string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, secondPerson.Name),
                    },
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveOpenUrlAction
                    {
                        Title = string.Format(Resources.ChatWithMatchButtonText, secondPersonFirstName),
                        Url = new Uri(chatMessageLink),
                    },
                    new AdaptiveOpenUrlAction
                    {
                        Title = Resources.ProposeMeetupButtonText,
                        Url = new Uri(meetingLink),
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
                Content = pairUpCard,
            };
        }

        /// <summary>
        /// Checks whether or not an account is a guest user.
        /// </summary>
        /// <param name="account">The <see cref="TeamsChannelAccount"/> user to check.</param>
        /// <returns>A value to indicate if the account is a guest user.</returns>
        private static bool IsGuestUser(TeamsChannelAccount account)
        {
            return account.UserPrincipalName.IndexOf("#ext#", StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}