<#
    .SYNOPSIS
    Automate deployment of Ice-Breaker app template
    .NOTES
    This involves validating for Azure region, resources names, creating AAD apps, deploying ARM templates and generating app manifests.
    
    .EXAMPLE
    .\deploy.ps1

-----------------------------------------------------------------------------------------------------------------------------------
Script name : deploy.ps1
Version : 1.0
Dependencies : Azure CLI, AzureAD, AZ, WriteAscii
-----------------------------------------------------------------------------------------------------------------------------------
-----------------------------------------------------------------------------------------------------------------------------------
DISCLAIMER
   THIS CODE IS SAMPLE CODE. THESE SAMPLES ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND.
   MICROSOFT FURTHER DISCLAIMS ALL IMPLIED WARRANTIES INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
   OF MERCHANTABILITY OR OF FITNESS FOR A PARTICULAR PURPOSE. THE ENTIRE RISK ARISING OUT OF THE USE OR
   PERFORMANCE OF THE SAMPLES REMAINS WITH YOU. IN NO EVENT SHALL MICROSOFT OR ITS SUPPLIERS BE LIABLE FOR
   ANY DAMAGES WHATSOEVER (INCLUDING, WITHOUT LIMITATION, DAMAGES FOR LOSS OF BUSINESS PROFITS, BUSINESS
   INTERRUPTION, LOSS OF BUSINESS INFORMATION, OR OTHER PECUNIARY LOSS) ARISING OUT OF THE USE OF OR
   INABILITY TO USE THE SAMPLES, EVEN IF MICROSOFT HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGES.
   BECAUSE SOME STATES DO NOT ALLOW THE EXCLUSION OR LIMITATION OF LIABILITY FOR CONSEQUENTIAL OR
   INCIDENTAL DAMAGES, THE ABOVE LIMITATION MAY NOT APPLY TO YOU.
#>

function WriteInfo {
    param(
        [parameter(mandatory = $true)]
        [string]$message
    )
    Write-Host $message -foregroundcolor white
}

function WriteError {
    param(
        [parameter(mandatory = $true)]
        [string]$message
    )
    Write-Host $message -foregroundcolor red -BackgroundColor black
}

function WriteWarning {
    param(
        [parameter(mandatory = $true)]
        [string]$message
    )
    Write-Host $message -foregroundcolor yellow -BackgroundColor black
}

function WriteSuccess {
    param(
        [parameter(mandatory = $true)]
        [string]$message
    )
    Write-Host $message -foregroundcolor green -BackgroundColor black
}

function IsValidSecureUrl {
    param(
        [Parameter(Mandatory = $true)] [string] $url
    )
    # Url with https prefix REGEX matching
    return ($url -match "https:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)")
}

function IsValidGuid {
    [OutputType([bool])]
    param
    (
        [Parameter(Mandatory = $true)]
        [string]$ObjectGuid
    )

    # Define verification regex
    [regex]$guidRegex = '(?im)^[{(]?[0-9A-F]{8}[-]?(?:[0-9A-F]{4}[-]?){3}[0-9A-F]{12}[)}]?$'

    # Check guid against regex
    return $ObjectGuid -match $guidRegex
}

function IsValidParameter {
    [OutputType([bool])]
    param
    (
        [Parameter(Mandatory = $true)]
        $param
    )

    return -not([string]::IsNullOrEmpty($param.Value)) -and ($param.Value -ne '<<value>>')
}

# Validate input parameters.
function ValidateParameters {
    $isValid = $true
    if (-not(IsValidParameter($parameters.subscriptionId))) {
        WriteError "Invalid subscriptionId."
        $isValid = $false;
    }

    if (-not(IsValidParameter($parameters.subscriptionTenantId)) -or -not(IsValidGuid -ObjectGuid $parameters.subscriptionTenantId.Value)) {
        WriteError "Invalid subscriptionTenantId. This should be a GUID."
        $isValid = $false;
    }

    if (-not (IsValidParameter($parameters.resourceGroupName))) {
        WriteError "Invalid resourceGroupName."
        $isValid = $false;
    }

    if (-not (IsValidParameter($parameters.location))) {
        WriteError "Invalid location."
        $isValid = $false;
    }

    if (-not (IsValidParameter($parameters.baseResourceName))) {
        WriteError "Invalid baseResourceName."
        $isValid = $false;
    }

    if (-not(IsValidParameter($parameters.tenantId)) -or -not(IsValidGuid -ObjectGuid $parameters.tenantId.Value)) {
        WriteE -message "Invalid tenantId. This should be a GUID."
        $isValid = $false;
    }

    if (-not(IsValidParameter($parameters.companyName))) {
        WriteError "Invalid companyName."
        $isValid = $false;
    }

    if (-not(IsValidSecureUrl($parameters.WebsiteUrl.Value))) {
        WriteError "Invalid websiteUrl. This should be an https url."
        $isValid = $false;
    }

    if (-not(IsValidSecureUrl($parameters.PrivacyUrl.Value))) {
        WriteError "Invalid PrivacyUrl. This should be an https url."
        $isValid = $false;
    }

    if (-not(IsValidSecureUrl($parameters.TermsOfUseUrl.Value))) {
        WriteError "Invalid TermsOfUseUrl. This should be an https url."
        $isValid = $false;
    }

    # Set default value for pairing key
    if (-not (IsValidParameter($parameters.pairingStartKey))) {
        $parameters.pairingStartKey.Value = (New-Guid).Guid
    }

    return $isValid
}

# To get the Azure AD app detail. 
function GetAzureADApp {
    param ($appName)
    $app = az ad app list --filter "displayName eq '$appName'" | ConvertFrom-Json
    return $app
}

# Create/re-set Azure AD app.
function CreateAzureADApp {
    param(
        [Parameter(Mandatory = $true)] [string] $AppName,
        [Parameter(Mandatory = $false)] [bool] $MultiTenant = $true,
        [Parameter(Mandatory = $false)] [bool] $AllowImplicitFlow = $false,
        [Parameter(Mandatory = $false)] [bool] $ResetAppSecret = $true
    )
        
    try {
        WriteInfo "`r`nCreating Azure AD App: $appName..."

        # Check if the app already exists - script has been previously executed
        $app = GetAzureADApp $appName

        if (-not ([string]::IsNullOrEmpty($app))) {

            # Update Azure AD app registration using CLI
            $confirmationTitle = "The Azure AD app '$appName' already exists. If you proceed, this will update the existing app configuration."
            $confirmationQuestion = "Do you want to proceed?"
            $confirmationChoices = "&Yes", "&No" # 0 = Yes, 1 = No
            
            $updateDecision = $Host.UI.PromptForChoice($confirmationTitle, $confirmationQuestion, $confirmationChoices, 1)
            if ($updateDecision -eq 0) {
                WriteInfo "Updating the existing app..."

                az ad app update --id $app.appId --available-to-other-tenants $MultiTenant --oauth2-allow-implicit-flow $AllowImplicitFlow

                WriteInfo "Waiting for app update to finish..."

                Start-Sleep -s 10

                WriteSuccess "Azure AD App: $appName is updated."
            } else {
                WriteError "Deployment canceled. Please use a different name for the Azure AD app and try again."
                return $null
            }
        } else {
            # Create Azure AD app registration using CLI
            az ad app create --display-name $appName --available-to-other-tenants $MultiTenant --oauth2-allow-implicit-flow $AllowImplicitFlow

            WriteInfo "Waiting for app creation to finish..."

            Start-Sleep -s 10

            WriteSuccess "Azure AD App: $appName is created."
        }

        $app = GetAzureADApp $appName
        
        $appSecret = $null;
        # Reset the app credentials to get the secret. The default validity of this secret will be for 1 year from the date its created. 
        if ($ResetAppSecret) {
            WriteInfo "Updating app secret..."
            $appSecret = az ad app credential reset --id $app.appId --append | ConvertFrom-Json;
        }

        WriteSuccess "Azure AD App: $appName registered successfully."
        return $appSecret
    }
    catch {
        $errorMessage = $_.Exception.Message
        WriteError "Failed to register/configure the Azure AD app. Error message: $errorMessage"
    }
    return $null
}

#to get the deployment log with the help of logged in user detail.
function CollectARMDeploymentLogs {
    $logsPath = '.\DeploymentLogs'
    $activityLogPath = "$logsPath\activity_log.log"
    $deploymentLogPath = "$logsPath\deployment_operation.log"

    $logsFolder = New-Item -ItemType Directory -Force -Path $logsPath

    az deployment operation group list --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value --name azuredeploy --query "[?properties.provisioningState=='Failed'].properties.statusMessage.error" | Set-Content $deploymentLogPath

    $activityLog = $null
    $retryCount = 5
    do {
        WriteInfo "Collecting deployment logs..."

        # Wait for async logs to persist
        Start-Sleep -s 30

        # Returns empty [] if logs are not available yet
        $activityLog = az monitor activity-log list -g $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value --caller $userAlias --status Failed --offset 30m

        $retryCount--

    } while (($activityLog.Length -lt 3) -and ($retryCount -gt 0))

    $activityLog | Set-Content $activityLogPath

    # collect web apps deployment logs
    $activityLogErrors = ($activityLog | ConvertFrom-Json) | Where-Object { ($null -ne $_.resourceType) -and ($_.resourceType.value -eq "Microsoft.Web/sites/sourcecontrols") }
    $resourcesLookup = @($activityLogErrors | Select-Object resourceId, @{Name = "resourceName"; Expression = { GetResourceName $_.resourceId } })
    if ($resourcesLookup.length -gt 0) {
        foreach ($resourceInfo in $resourcesLookup) {
            if ($null -ne $resourceInfo.resourceName) {
                az webapp log download --ids $resourceInfo.resourceId --log-file "$logsPath\$($resourceInfo.resourceName).zip"
            }
        }
    }
    
    # Generate zip archive and delete folder
    $compressManifest = @{
        Path             = $logsPath
        CompressionLevel = "Fastest"
        DestinationPath  = "logs.zip"
    }
    Compress-Archive @compressManifest -Force
    Get-ChildItem -Path $logsPath -Recurse | Remove-Item -Force -Recurse -ErrorAction Continue
    Remove-Item $logsPath -Force -ErrorAction Continue
    
    WriteInfo "Deployment logs generation finished. Please share Deployment\logs.zip file with the app template team to investigate..."
}

function WaitForCodeDeploymentSync {
    Param(
        [Parameter(Mandatory = $true)] $appServicesName
    )

    $appserviceCodeSyncSuccess = $true
    $codeSyncPending = $true
    while($codeSyncPending)
    {
        WriteInfo "Checking source control deployment progress..."
        $deploymentResponse = az rest --method get --uri /subscriptions/$($parameters.subscriptionId.Value)/resourcegroups/$($parameters.resourceGroupName.Value)/providers/Microsoft.Web/sites/$appServicesName/deployments?api-version=2019-08-01 | ConvertFrom-Json
        $deploymentsList = $deploymentResponse.value
        if($deploymentsList.length -eq 0 -or $deploymentsList[0].properties.complete){
            $appserviceCodeSyncSuccess = $appserviceCodeSyncSuccess -and ($deploymentsList.length -eq 0 -or $deploymentsList[0].properties.status -ne 3) # 3 means sync fail
            $codeSyncPending = $false
        }

        WriteInfo "Source control deployment is still in progress. Next check in 1 minute."
        Start-Sleep -Seconds 60
    }
    if($appserviceCodeSyncSuccess){
        WriteInfo "Source control deployment is done."
    } else {
        WriteError "Source control deployment failed."
    }
    return $appserviceCodeSyncSuccess
}

function DeployARMTemplate {
    Param(
        [Parameter(Mandatory = $true)] $userappId,
        [Parameter(Mandatory = $true)] $usersecret
    )
    try {
        if ((az group exists --name $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value) -eq $false) {
            WriteInfo "Creating resource group $($parameters.resourceGroupName.Value)..."
            az group create --name $parameters.resourceGroupName.Value --location $parameters.location.Value --subscription $parameters.subscriptionId.Value
        }

        WriteInfo "Scan $($parameters.baseResourceName.Value) source control configuration for conflicts"
        $appServices = az resource list --resource-group  $parameters.resourceGroupName.Value --query "[?type=='Microsoft.Web/sites']" --subscription $parameters.subscriptionId.Value
        if($appServices){
            $appServices = $appServices | ConvertFrom-Json
            if($appServices.length -gt 0){
                $appServiceName = $appServices[0].name
                $deploymentConfig = az webapp deployment source show --name $appServiceName --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value
                if($deploymentConfig) {
                    $deploymentConfig = $deploymentConfig | ConvertFrom-Json
                    # conflicts in branches, clear old configuraiton
                    if(($deploymentConfig.branch -ne $parameters.gitBranch.Value) -or ($deploymentConfig.repoUrl -ne $parameters.gitRepoUrl.Value)){
                        WriteInfo "Remove $($parameters.baseResourceName.Value) source control configuration"
                        az webapp deployment source delete --name $appServiceName --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value
                    }
                }
                else {
                    # If command failed due to resource not exists, then screen colors is becoming red
                    [Console]::ResetColor()
                }
            }
        }

        # Deploy ARM templates
        WriteInfo "`nDeploying app services, Azure function, bot service, and other supporting resources..."
        $armDeploymentResult = az deployment group create --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value --template-file 'azuredeploy.json' --parameters "botAppID=$userappId" "botAppPassword=$usersecret" "appName=$($parameters.baseResourceName.Value)" "tenantId=$($parameters.tenantId.Value)" "appDescription=$($parameters.appDescription.Value)" "appIconUrl=$($parameters.appIconUrl.Value)" "pairingWeekInterval=$($parameters.pairingWeekInterval.Value)" "pairingDayOfWeek=$($parameters.pairingDayOfWeek.Value)" "pairingHour=$($parameters.pairingHour.Value)" "pairingTimeZone=$($parameters.pairingTimeZone.Value)" "pairingStartKey=$($parameters.pairingStartKey.Value)" "sku=$($parameters.sku.Value)" "planSize=$($parameters.planSize.Value)" "location=$($parameters.location.Value)" "gitRepoUrl=$($parameters.gitRepoUrl.Value)" "gitBranch=$($parameters.gitBranch.Value)" "DefaultCulture=$($parameters.defaultCulture.Value)"

        $deploymentExceptionMessage = "ERROR: ARM template deployment error."
        if ($LASTEXITCODE -ne 0) {
            # If ARM template deployment failed for any reason, then screen colors becomes red
            [Console]::ResetColor()
            CollectARMDeploymentLogs
            Throw $deploymentExceptionMessage
        }
        
        # get the output of current deployment
        $deploymentOutput = az deployment group show --name azuredeploy --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value | ConvertFrom-Json
        $appServiceName = $deploymentOutput.properties.Outputs.appServiceName.Value
        
        WriteSuccess "Finished deploying resources. ARM template deployment succeeded."

        # sync app services code deployment (ARM deployment will not sync automatically)
        WriteInfo "Sync $($parameters.baseResourceName.Value) code from latest version"
        az webapp deployment source sync --name $($appServiceName) --resource-group $parameters.resourceGroupName.Value --subscription $parameters.subscriptionId.Value
        
        # sync command is async. Wait for source control sync to finish
        $appserviceCodeSyncSuccess = WaitForCodeDeploymentSync $appServiceName
        if(-not $appserviceCodeSyncSuccess){
            CollectARMDeploymentLogs
            Throw $deploymentExceptionMessage
        }
        
        return $deploymentOutput
    }
    catch {
        WriteError "Error occurred while deploying Azure resources."
        throw
    }
}

# Update manifest file and create a .zip file.
function GenerateAppManifestPackage {
    Param(
        [Parameter(Mandatory = $true)] $appdomainName,
        [Parameter(Mandatory = $true)] $appId
    )

        WriteInfo "`nGenerating package for the app template..."

        $sourceManifestPath = "..\Manifest\manifest.json"
        $srcManifestBackupPath = "..\Manifest\manifest_backup.json"
        $destinationZipPath = "..\manifest\IceBreaker-manifest.zip"
        
        if (!(Test-Path $sourceManifestPath)) {
            throw "$sourceManifestPath does not exist. Please make sure you download the full app template source."
        }

        copy-item -path $sourceManifestPath -destination $srcManifestBackupPath -Force

        # Replace merge fields with proper values in manifest file and save
        $mergeFields = @{
            '<company name>'   = $parameters.companyName.Value 
            '<bot id>'         = $appId
            '<app domain>'     = $appdomainName
            '<website url>'    = $parameters.websiteUrl.Value
            '<privacy url>'    = $parameters.privacyUrl.Value
            '<terms of use url>' = $parameters.termsOfUseUrl.Value
        }
        $appManifestContent = Get-Content $sourceManifestPath
        foreach ($mergeField in $mergeFields.GetEnumerator()) {
            $appManifestContent = $appManifestContent.replace($mergeField.Name, $mergeField.Value)
        }
        $appManifestContent | Set-Content $sourceManifestPath -Force

        # Generate zip archive 
        $compressManifest = @{
            LiteralPath      = "..\manifest\color.png", "..\manifest\outline.png", $sourceManifestPath
            CompressionLevel = "Fastest"
            DestinationPath  = $destinationZipPath
        }
        
        Compress-Archive @compressManifest -Force

        Remove-Item $sourceManifestPath -ErrorAction Continue

        Rename-Item -Path $srcManifestBackupPath -NewName 'manifest.json'

        WriteSuccess "Package has been created under this path $(Resolve-Path $destinationZipPath)"
}

function logout {
    $logOut = az logout
    $disAzAcc = Disconnect-AzAccount
}

function InstallDependencies {
    
    # Check if Azure CLI is installed.
    WriteInfo "Checking if Azure CLI is installed."
    $localPath = [Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if ($null -eq $localPath) {
        $localPath = "C:\Program Files (x86)"
    }

    $localPath = $localPath + "\Microsoft SDKs\Azure\CLI2"
    If (-not(Test-Path -Path $localPath)) {
        WriteWarning "Azure CLI is not installed!"
        $confirmationtitle      = "Please select YES to install Azure CLI."
        $confirmationquestion   = "Do you want to proceed?"
        $confirmationchoices    = "&yes", "&no" # 0 = yes, 1 = no
            
        $updatedecision = $host.ui.promptforchoice($confirmationtitle, $confirmationquestion, $confirmationchoices, 1)
        if ($updatedecision -eq 0) {
            WriteInfo "Installing Azure CLI ..."
            Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'; rm .\AzureCLI.msi
            WriteSuccess "Azure CLI is installed! Please close this PowerShell window and re-run this script in a new PowerShell session."
            EXIT
        } else {
            WriteError "Azure CLI is not installed.`nPlease install the CLI from https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest and re-run this script in a new PowerShell session"
            EXIT
        }
    } else {
        WriteSuccess "Azure CLI is installed."
    }

    # Installing required modules
    WriteInfo "Checking if the required modules are installed..."
    $isAvailable = $true
    if ((Get-Module -ListAvailable -Name "Az.*")) {
        WriteInfo "Az module is available."
    } else {
        WriteWarning "Az module is missing."
        $isAvailable = $false
    }

    if ((Get-Module -ListAvailable -Name "AzureAD")) {
        WriteInfo "AzureAD module is available."
    } else {
        WriteWarning "AzureAD module is missing."
        $isAvailable = $false
    }

    if ((Get-Module -ListAvailable -Name "WriteAscii")) {
        WriteInfo "WriteAscii module is available."
    } else {
        WriteWarning "WriteAscii module is missing."
        $isAvailable = $false
    }

    if (-not $isAvailable)
    {
        $confirmationTitle = WriteInfo "The script requires the following modules to deploy: `n 1.Az module`n 2.AzureAD module `n 3.WriteAscii module`nIf you proceed, the script will install the missing modules."
        $confirmationQuestion = "Do you want to proceed?"
        $confirmationChoices = "&Yes", "&No" # 0 = Yes, 1 = No
                
        $updateDecision = $Host.UI.PromptForChoice($confirmationTitle, $confirmationQuestion, $confirmationChoices, 1)
            if ($updateDecision -eq 0) {
                if (-not (Get-Module -ListAvailable -Name "Az.*")) {
                    WriteInfo "Installing AZ module..."
                    Install-Module Az -AllowClobber -Scope CurrentUser
                }

                if (-not (Get-Module -ListAvailable -Name "AzureAD")) {
                    WriteInfo "Installing AzureAD module..."
                    Install-Module AzureAD -Scope CurrentUser -Force
                }
                
                if (-not (Get-Module -ListAvailable -Name "WriteAscii")) {
                    WriteInfo "Installing WriteAscii module..."
                    Install-Module WriteAscii -Scope CurrentUser -Force
                }
            } else {
                WriteError "You may install the modules manually by following the below link. Please re-run the script after the modules are installed. `nhttps://docs.microsoft.com/en-us/powershell/module/powershellget/install-module?view=powershell-7"
                EXIT
            }
    } else {
        WriteSuccess "All the modules are available!"
    }
}

# ---------------------------------------------------------
# DEPLOYMENT SCRIPT
# ---------------------------------------------------------
    # Check for dependencies and install if needed
    InstallDependencies -ErrorAction Stop
    
    # Load Parameters from JSON meta-data file
    $parametersListContent = Get-Content '.\parameters.json' -ErrorAction Stop

    # Validate all the parameters.
    WriteInfo "Validating all the parameters from parameters.json."
    $parameters = $parametersListContent | ConvertFrom-Json
    if (-not(ValidateParameters)) {
        WriteError "Invalid parameters found. Please update the parameters in the parameters.json with valid values and re-run the script."
        EXIT
    }

    # Start Deployment.
    Write-Ascii -InputObject "Ice-Breaker V2" -ForegroundColor Magenta
    WriteInfo "Starting deployment..."

    # Initialize connections - Azure Az/CLI/Azure AD
    WriteInfo "Login with with your Azure subscription account. Launching Azure sign-in window..."
    Connect-AzAccount -Subscription $parameters.subscriptionId.Value -ErrorAction Stop
    $user = az login --tenant $parameters.subscriptionTenantId.value
    if ($LASTEXITCODE -ne 0) {
        WriteError "Login failed for user..."
        EXIT
    }

    $userAlias = (($user | ConvertFrom-Json) | where {$_.id -eq $parameters.subscriptionId.Value}).user.name

    # Create User App
    $userAppCred = CreateAzureADApp $parameters.baseResourceName.Value
    if ($null -eq $userAppCred) {
        WriteError "Failed to create or update the app in Azure Active Directory. Exiting..."
        logout
        Exit
    }

    # Function call to Deploy ARM Template
    $deploymentOutput = DeployARMTemplate $userAppCred.appId $userAppCred.password
    if ($null -eq $deploymentOutput) {
        WriteError "Encountered an error during ARM template deployment. Exiting..."
        logout
        Exit
    }

    # Assigning return values to variable. 
    $appdomainName = $deploymentOutput.properties.Outputs.appDomain.Value

    # Log out to avoid tokens caching
    logout

    # Function call to generate manifest.zip folder for User and Author. 
    GenerateAppManifestPackage $appdomainName $userAppCred.appId

    # Open manifest folder
    Invoke-Item ..\Manifest\

    # Deployment completed.
    Write-Ascii -InputObject "DEPLOYMENT COMPLETED." -ForegroundColor Green
