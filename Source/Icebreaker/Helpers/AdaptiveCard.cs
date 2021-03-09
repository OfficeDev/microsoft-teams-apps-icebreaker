// <copyright file="AdaptiveCard.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    /// <summary>
    /// Enum members represent the different cards the bot is sending
    /// </summary>
    public enum AdaptiveCardName
    {
        /// <summary>
        /// Represents the pair-up card sent after matching
        /// </summary>
        PairUpNotification,

        /// <summary>
        /// Represents unrecognized card when bot receives non-supported command
        /// </summary>
        UnrecognizedInput,

        /// <summary>
        /// Represents the welcome user card sent to team member in direct chat
        /// </summary>
        WelcomeNewMember,

        /// <summary>
        /// Represents the welcome card sent to team channel once bot is added
        /// </summary>
        WelcomeTeam,
    }
}