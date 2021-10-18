// <copyright file="TourController.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Controllers
{
    using System.Globalization;
    using System.Threading;
    using Icebreaker.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure;

    /// <summary>
    /// Serves default tour content
    /// </summary>
    public class TourController : Controller
    {
        private readonly IAppSettings appSettings;

        public TourController(IAppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        /// <summary>
        /// Serves tour content after localization
        /// </summary>
        /// <param name="locale">User locale in MSTeams</param>
        /// <returns>Tour content</returns>
        [HttpGet]
        public ActionResult Index(string locale)
        {
            if (!string.IsNullOrEmpty(locale))
            {
                CultureInfo culture;
                try
                {
                    culture = CultureInfo.GetCultureInfo(locale);
                }
                catch
                {
                    // Fall back to the default culture setting if there is an error getting a CultureInfo from the locale
                    culture = CultureInfo.GetCultureInfo(this.appSettings.DefaultCulture);
                }

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
            }

            return this.View();
        }
    }
}