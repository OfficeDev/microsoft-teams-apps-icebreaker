# Icebreaker Version 3

The major updates in V3 are 
- Updating the .net framework sdk to .net core 3.1. Deploying V3 code takes care of this upgrade.
- Introducing optional cert based bot authentication.

## Cert based bot authentiation
- Uptill V2, Icebreaker bot uses password based authentication. In the version V3, the deployment facilitates an optional cert based authentication method. 
- 1) Post deployment (After excecuting ./deploy.ps1 - https://github.com/OfficeDev/microsoft-teams-apps-icebreaker/wiki/Deployment-guide or https://github.com/OfficeDev/microsoft-teams-apps-icebreaker/wiki/Deployment-guide-manual) make a note of the **resource group and AppService** thus created. 
- 2) Go to the AppService in Azure Portal. Under **App Service -> Configuration** a parameter named "BotCertName" will be created with an empty string value.

<img width="1189" alt="image" src="https://user-images.githubusercontent.com/86118493/144939129-9736b2b1-dc64-4f3c-801c-4ed2338b64b3.png">

- 3) Go to the Resource group noted in the step 1. In order to use cert based auth, a certificate should be created under the Key Vault under this resource group. Make a note of the **Certificate's name**.
- 4) Replace the empty string from step 2) with the the certificate name noted from the above step.
