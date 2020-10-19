Continuous Deployment service in Azure App Services and Azure Function Apps offers merging latest code changes to Azure hosted environment from GitHub. It helps to seamlessly update the Azure services without need of new deployment.

### Continuous deployment in Azure App Services

Please follow below steps to deploy latest changes to the app service:

1. Log in to the Azure Portal for your subscription.

1. Select App Services from left menu blade

    [[https://github.com/OfficeDev/microsoft-teams-apps-bookaroom/wiki/Images/Azure-appservice-menu.png|Azure app service menu blade]]

1. Search and select the app service name (search for the `base resource name`) which is created during first deployment. For e.g. `constoso-faqplusv2`.azurewebsites.net

1. Select Deployment Center under menu blade

    [[https://github.com/OfficeDev/microsoft-teams-apps-bookaroom/wiki/Images/Deployment-center.png|Deployment center in Azure app service]]

1. Click on Sync to synchronize the latest bits from GitHub master branch

    [[https://github.com/OfficeDev/microsoft-teams-apps-bookaroom/wiki/Images/sync-github.png|Sync GitHub deployment]]

    _note_: please make sure that `Repository` name is pointing to correct OfficeDev repo git path.

1. Once the deployment is successful, please restart the app service and check the application is working.  

### Continuous deployment in Azure Function Apps 

Please follow below steps to deploy latest changes to the app service:

1. Search Function App in Azure portal search box and select.

1. Filter the app name which is created during first deployment. For e.g. `constoso-faqplusv2`-function.azurewebsites.net
    [[https://github.com/OfficeDev/microsoft-teams-apps-bookaroom/wiki/Images/Azure-function-menu.png|Azure Function App Overview menu]]

1. Under Overview section, select `Deployment options configured with ExternalGit`

1. Under Deployment Center, click on Sync to synchronize the latest bits from GitHub master branch

_note_: please make sure that `Repository` name is pointing to correct OfficeDev repo git path.