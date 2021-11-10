// <copyright file="RetryPolicyHelper.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Helpers
{
    using System;
    using Microsoft.Bot.Schema;
    using Microsoft.Extensions.Logging;
    using Polly;
    using Polly.Contrib.WaitAndRetry;
    using Polly.Retry;

    /// <summary>
    /// RetryPolicyHelper sets the policy for Polly's retry
    /// </summary>
    public class RetryPolicyHelper
    {
        private const int MaxRetry = 3;
        private const int MedianFirstRetryDelayInSeconds = 1; // seconds
        private static readonly int[] RetryStatusCodes = { 429, 500, 502, 503, 504 };

        /// <summary>
        /// A static method to get retry policy for IceBreaker
        /// </summary>
        /// <param name="logger">logger to use</param>
        /// <returns>Polly's AsyncRetryPolicy</returns>
        public static AsyncRetryPolicy GetRetryPolicy(ILogger logger)
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(medianFirstRetryDelay: TimeSpan.FromSeconds(MedianFirstRetryDelayInSeconds), retryCount: MaxRetry);
            return Policy.Handle<ErrorResponseException>(e =>
            {
                logger.LogWarning(e, $"Exception thrown: {e.GetType()}: {e.Message}");

                // Handle throttling and internal server errors.
                var statusCode = e.Response.StatusCode;
                return Array.IndexOf(RetryStatusCodes, (int)statusCode) >= 0;
            }).WaitAndRetryAsync(delay);
        }
    }
}
