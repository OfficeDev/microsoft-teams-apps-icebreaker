// <copyright file="SecretsProvider.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Icebreaker.Secrets
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Secrets;
    using Icebreaker.Interfaces;
    using Microsoft.ApplicationInsights;
    using Microsoft.Bot.Connector.Authentication;

    /// <summary>
    /// Secrets provider implementation.
    /// </summary>
    public class SecretsProvider : ISecretsProvider
    {
        private readonly IAppSettings appSettings;
        private readonly SecretOptions options;
        private readonly bool readFromKV;

        private readonly SecretClient secretClient;

        private readonly TelemetryClient telemetryClient;

        private readonly CertificateClient certificateClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="SecretsProvider"/> class.
        /// </summary>
        /// <param name="appSettings">Appsettings</param>
        /// <param name="options">Secret options.</param>
        /// <param name="telemetryClient">For logging</param>
        /// <param name="secretClient">To access KV</param>
        /// <param name="certificateClient">To read cert</param>
        public SecretsProvider(
            IAppSettings appSettings,
            SecretOptions options,
            TelemetryClient telemetryClient,
            SecretClient secretClient,
            CertificateClient certificateClient)
        {
            this.appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            this.options = options ?? throw new ArgumentNullException(nameof(options));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            this.readFromKV = !string.IsNullOrEmpty(this.options.KeyVaultUri);
            this.secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
            this.certificateClient = certificateClient ?? throw new ArgumentNullException(nameof(certificateClient));
        }

        /// <inheritdoc/>
        public string GetCosmosDBKey()
        {
            if (!this.readFromKV)
            {
                return this.options.CosmosDBKey;
            }

            // else read from KV
            return this.ReadSecretsFromKV(this.appSettings.CosmosDBKeyName);
        }

        /// <inheritdoc/>
        public string GetLogicAppKey()
        {
            if (!this.readFromKV)
            {
                return this.options.Key;
            }

            // else read from KV
            return this.ReadSecretsFromKV(this.appSettings.ParingKeyName);
        }

        /// <inheritdoc/>
        public async Task<AppCredentials> GetAppCredentialsAsync()
        {
            var botAppId = this.appSettings.MicrosoftAppId;

            // If certificate name is configured, download from KeyVault.
            if (!string.IsNullOrEmpty(this.appSettings.BotCertName))
            {
                this.telemetryClient.TrackTrace("Using cert based auth");
                var cert = await this.DownloadCertificate();
                return new CertificateAppCredentials(cert, botAppId);
            }

            // Else fallback on App Id/Secrets
            this.telemetryClient.TrackTrace("Using secrets based auth");
            return new MicrosoftAppCredentials(this.appSettings.MicrosoftAppId, this.GetMicrosoftAppPassword());
        }

        private string GetMicrosoftAppPassword()
        {
            if (!this.readFromKV)
            {
                this.telemetryClient.TrackTrace("Reading app password from appSettings");
                return this.options.MicrosoftAppPassword;
            }

            // else read from KV
            this.telemetryClient.TrackTrace("Reading app password from KV");
            return this.ReadSecretsFromKV(this.appSettings.MicrosoftAppPasswordKeyName);
        }

        private string ReadSecretsFromKV(string key)
        {
            try
            {
                this.telemetryClient.TrackTrace($"Reading {key} from Secrets");
                var secretValue = this.secretClient.GetSecret(key).Value?.Value;
                this.telemetryClient.TrackTrace("Secret value null or empty ? " + String.IsNullOrEmpty(secretValue) + "\n" + "Secret value null or whitespace ? " + String.IsNullOrWhiteSpace(secretValue));
                return secretValue;
            }
            catch (Exception exception)
            {
                this.telemetryClient.TrackTrace("Exception while reading secrets from KV" + exception.ToString());
                this.telemetryClient.TrackException(exception.InnerException);
                throw;
            }
        }

        private async Task<X509Certificate2> DownloadCertificate()
        {
            try
            {
                this.telemetryClient.TrackTrace($"About to download the certificate from the KeyVault with uri {this.options.KeyVaultUri}, CertName: {this.appSettings.BotCertName}");
                var cert = await this.certificateClient.DownloadCertificateAsync(this.appSettings.BotCertName);
                return cert.Value;
            }
            catch (Exception exception)
            {
                this.telemetryClient.TrackException(exception.InnerException);
                throw new Exception($"Failed to download certificate. Certificate name: {this.appSettings.BotCertName}", exception);
            }
        }
    }
}