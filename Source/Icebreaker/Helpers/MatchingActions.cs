// <copyright file="MatchingActions.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    /// <summary>
    /// Provide possible actions user can take
    /// </summary>
    public class MatchingActions
    {
        /// <summary>
        /// Opt-in for pairs matching
        /// </summary>
        public const string OptIn = "optin";

        /// <summary>
        /// Opt-out for pairs matching
        /// </summary>
        public const string OptOut = "optout";

        /// <summary>
        /// Report as inactive
        /// </summary>
        public const string ReportInactive = "inactive";

        /// <summary>
        /// Confirm inactive trigger opt out
        /// </summary>
        public const string ConfirmInactive = "confirmInactive";
    }
}