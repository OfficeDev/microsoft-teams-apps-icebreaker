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
        /// <param name="sender">The first person in the pair up.</param>
        /// <param name="recipient">The second person in the pair up.</param>
        /// <param name="botDisplayName">The bot display name.</param>
        public static Attachment GetCard(
            string teamName,
            TeamsChannelAccount sender,
            TeamsChannelAccount recipient,
            string botDisplayName)
        {
            var senderGivenName = string.IsNullOrEmpty(sender.GivenName) ? sender.Name : sender.GivenName;
            var recipientGivenName = string.IsNullOrEmpty(recipient.GivenName) ? recipient.Name : recipient.GivenName;
            var title = string.Format(Resources.MeetupTitle, senderGivenName, recipientGivenName);

            var escapedTitle = Uri.EscapeDataString(title);
            var content = string.Format(Resources.MeetupContent, botDisplayName);
            var escapedContent = Uri.EscapeDataString(content);

            var recipientUpn = !IsGuestUser(recipient) ? recipient.UserPrincipalName : recipient.Email;
            var meetingLink = Uri.EscapeDataString("https://teams.microsoft.com/l/meeting/new?subject=" + escapedTitle + "&attendees=" + recipientUpn + "&content=" + escapedContent);
            var chatMessageLink = Uri.EscapeDataString($"https://teams.microsoft.com/l/chat/0/0?users={recipientUpn}&message=Hi%20there%20");

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
                        Text = string.Format(Resources.MatchUpCardMatchedText, recipient.Name),
                    },
                    new AdaptiveTextBlock
                    {
                        Wrap = true,
                        Text = string.Format(Resources.MatchUpCardContentPart1, botDisplayName, teamName, recipient.Name),
                    },
                },
                Actions = BuildActionList(IsGuestUser(recipient), chatMessageLink, recipientGivenName, meetingLink),
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

        /// <summary>
        /// Building the actions list for the pairup card.
        /// </summary>
        /// <param name="isGuestUser">A boolean value determining whether or not a user is a guest.</param>
        /// <param name="chatLink">The deeplink for the chat</param>
        /// <param name="receiverName">The receiver name.</param>
        /// <param name="meetingLink">The meeting link for proposing a meetup.</param>
        /// <returns>A list of actions.</returns>
        private static List<AdaptiveAction> BuildActionList(bool isGuestUser, string chatLink, string receiverName, string meetingLink)
        {
            var actionList = new List<AdaptiveAction>();

            actionList.Add(new AdaptiveOpenUrlAction
            {
                Title = string.Format(Resources.ChatWithMatchButtonText, receiverName),
                Url = new Uri(chatLink),
            });

            if (!isGuestUser)
            {
                actionList.Add(new AdaptiveOpenUrlAction
                {
                    Title = Resources.ProposeMeetupButtonText,
                    Url = new Uri(meetingLink),
                });
            }

            actionList.Add(new AdaptiveSubmitAction
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
            });

            return actionList;
        }
    }
}