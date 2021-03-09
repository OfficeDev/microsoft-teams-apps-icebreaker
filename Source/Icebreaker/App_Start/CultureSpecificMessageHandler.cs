// <copyright file="CultureSpecificMessageHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker
{
    using System.Globalization;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure;

    /// <summary>
    /// Message handler to set culture specific settings.
    /// </summary>
    public class CultureSpecificMessageHandler : DelegatingHandler
    {
        /// <inheritdoc/>
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cultureName = CloudConfigurationManager.GetSetting("DefaultCulture");
            var culture = new CultureInfo(cultureName);
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            return base.SendAsync(request, cancellationToken);
        }
    }
}