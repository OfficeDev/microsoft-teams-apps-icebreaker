// <copyright file="AdapterWithErrorHandler.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Icebreaker
{
    using System.Web.Mvc;
    using Icebreaker.Properties;
    using Microsoft.Bot.Builder.Integration.AspNet.WebApi;
    using Microsoft.Bot.Connector.Authentication;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Adapter to handle the bot errors.
    /// </summary>
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterWithErrorHandler"/> class.
        /// </summary>
        /// <param name="credentialProvider">The credential provider.</param>
        public AdapterWithErrorHandler(ICredentialProvider credentialProvider)
            : base(credentialProvider)
        {
            this.OnTurnError = async (turnContext, exception) =>
            {
                var logProvider = DependencyResolver.Current.GetService<ILogger>();
                logProvider.LogError($"Exception caught : {exception.Message}");

                // Send a catch-all apology to the user.
                await turnContext.SendActivityAsync(Resources.UnknownErrorMessage);
            };
        }
    }
}