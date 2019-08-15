//----------------------------------------------------------------------------------------------
// <copyright file="FilterConfig.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------

namespace Icebreaker
{
    using System.Web.Mvc;

    /// <summary>
    /// Filter Config class
    /// </summary>
    public class FilterConfig
    {
        /// <summary>
        /// RegisterGlobalFilters method
        /// </summary>
        /// <param name="filters">Global Filter Collection</param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
