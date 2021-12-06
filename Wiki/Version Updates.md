# Icebreaker Version 3

The major updates in V3 are 
- Updating the .net framework sdk to .net core 3.1. Deploying V3 code takes care of this upgrade.
- Introducing optional cert based bot authentication.
- Storing and retrieving secrets from Key Vault.

## Cert based bot authentiation
- Uptill V2, Icebreaker bot uses password based authentication. In the version V3, the deployment facilitates an optional cert based authentication method. Post deployment by running ./deploy.ps1 (Link) a parameter named "BotCertName" will be created with an empty string value under the AppService's configuration. 
- In order to switch over to a cert based authentication, the admin needs to create a certificate, update the above mentioned empty string value with the name ofthe certificate created.