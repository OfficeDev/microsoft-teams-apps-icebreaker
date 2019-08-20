// <copyright file="Extensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;

    /// <summary>
    /// This class contains custom methods that are being used.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Method that will look at having a specific text string, and having this done with case insensitive.
        /// </summary>
        /// <param name="text">The actual text to parse.</param>
        /// <param name="value">The string that we are looking for.</param>
        /// <param name="stringComparison">The string comparator.</param>
        /// <returns>A value saying whether or not a specific string exists.</returns>
        public static bool CaseInsensitiveContains(
            this string text,
            string value,
            StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
        {
            return text.IndexOf(value, stringComparison) >= 0;
        }
    }
}