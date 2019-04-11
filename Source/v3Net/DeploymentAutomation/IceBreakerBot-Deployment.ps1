<#
    <copyright file="MeetupBot-Deployment-AzureRM.ps1" company="Microsoft">
    // Copyright (c) Microsoft. All rights reserved.
    </copyright>
#>

# Allow for this PowerShell script to be run
Set-ExecutionPolicy -ExecutionPolicy Unrestricted -Scope CurrentUser

# Install the necessary modules
Install-Module AzureAD -RequiredVersion 2.0.2.16 -AllowClobber -Scope CurrentUser
Install-Module AzureRM -AllowClobber -Scope CurrentUser

# Prompting the user to authenticate and log in before moving forward
Connect-AzureAD

# Get the name of the application
$appName = Read-Host -Prompt "Enter the name of your new app"

# Get the description
$appDescription = Read-Host -Prompt "Give your app a description"

#Get the iconUrl
$iconUrl = Read-Host -Prompt "Give the url of the icon that you want to use"

# Enabling this to be a multi-tenanted application
$isMultiTenant = $true

$appVars = New-AzureADApplication -DisplayName $appName -AvailableToOtherTenants $isMultiTenant

# Get the app logo
$appLogoFolder = Get-Location
$appLogoLocation = -Join($appLogoFolder, '\Assets\logo.jpg')

# Get the Repo URL and branch
$repoUrl = Read-Host -Prompt "Please provide the GitHub URL for your app"
$branch = Read-Host -Prompt "Enter the branch name (i.e. master)"

# Getting the feedback email address
$contactEmail = Read-Host -Prompt "Provide an email address to use for providing feedback"

# Getting the details for the logic app
$logicAppFrequency = Read-Host -Prompt "Enter how frequently this app will pair up team members (i.e. Month, or Week)"
$logicAppInterval = [int](Read-Host -Prompt "Enter the interval (i.e. an integer value. If you enter 1 with a frequency of Week then the pairing will happen weekly [recommended])")
$logicAppHour = [int](Read-Host -Prompt "Enter a value for the hour between 0 and 23")
$logicAppMins = [int](Read-Host -Prompt "Enter a value for the minutes between 0 and 59")
$logicAppDOW = Read-Host -Prompt "Enter a day of the week when the the bot will pair the team members"
$logicAppTimeZone = Read-Host -Prompt "Enter the timezone where you are located (i.e. If you are in Seattle, WA use Pacific Standard Time)"

$pwdVars = New-AzureADApplicationPasswordCredential -ObjectId $appVars.ObjectId

$keyGUID = New-Guid;

#Set the logo for the application
Set-AzureADApplicationLogo -ObjectId $appVars.ObjectId -FilePath $appLogoLocation

# Starting to construct the parameters.azuredeploy.json file now
$schemaLink = "https://schema.management.azure.com/schemas/2015-01-01/deploymentParameters.json#"
$contentVer = "1.0.0.0"

$ParametersTemplate = @{
    '$schema' = $schemaLink;
    contentVersion = $contentVer;
    parameters = @{
        botAppID = @{
            "value" = $appVars.AppId
        }
        botApplicationPassword = @{
            "value" = $pwdVars.Value
        }
        botDisplayName = @{
            "value" = $appName
        }
        botDescription = @{
            "value" = $appDescription
        }
        iconUrl = @{
            "value" = $iconUrl
        }
        key = @{
            "value" = $keyGUID.Guid
        }
        repoUrl = @{
            "value" = $repoUrl
        }
        branch = @{
            "value" = $branch
        }
        feedbackEmail = @{
            "value" = $contactEmail
        }
        logicAppFrequency = @{
            "value" = $logicAppFrequency
        }
        logicAppInterval = @{
            "value" = $logicAppInterval
        }
        logicAppHour = @{
            "value" = $logicAppHour
        }
        logicAppMins = @{
            "value" = $logicAppMins
        }
        logicAppDOW = @{
            "value" = $logicAppDOW
        }
        logicAppTimeZone = @{
            "value" = $logicAppTimeZone
        }
    }
}

# Generating the output file
$outputFolder = Get-Location
$outputFileLoc = -Join($outputFolder, '\deploymentParameters.json')

$ParametersTemplate | ConvertTo-Json -Depth 5 | Out-File $outputFileLoc

<#
    Having the deployment done correctly
#>

# Get the name of the resource group - could be either existing resource group or a new resource group
$resourceGroupName = Read-Host -Prompt "Please enter the name of the Resource Group"

# Running a check to see if the resource group actually exists or not
Get-AzureRmResourceGroup -Name $resourceGroupName -ErrorVariable isPresent -ErrorAction SilentlyContinue

if ($isPresent)
{
    Write-Host "Cannot find a Resource Group with the name "$resourceGroupName
    Write-Host "That's okay! Creating the new Resource Group named "$resourceGroupName

    $location = Read-Host -Prompt "Enter a location (i.e. centralus) for the location of the Resource Group"

    $deploymentName = Read-Host -Prompt "Enter a name for the deployment"

    # Making sure to get the necessary information for the template deployment to happen
    $folderLocation = $outputFolder
    $templateFile = -Join($folderLocation, '\azuredeploy.json')
    $templateParamFile = $outputFileLoc

    # Create the new Resource Group
    New-AzureRmResourceGroup -Name $resourceGroupName -Location $location

    # Handling the deployment now
    New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $deploymentName -TemplateFile $templateFile -TemplateParameterFile $templateParamFile
}
else 
{
    Write-Host "Found the Resource Group "$resourceGroupName
    Write-Host "Continuing the deployment to "$resourceGroupName

    $deploymentName = Read-Host -Prompt "Enter a name for the deployment"

    $folderLocation = $outputFolder
    $templateFile = -Join($folderLocation, '\azuredeploy.json')
    $templateParamFile = -Join($folderLocation, '\deploymentParameters.json')

    # Continuing with the deployment now
    New-AzureRmResourceGroupDeployment -ResourceGroupName $resourceGroupName -Name $deploymentName -TemplateFile $templateFile -TemplateParameterFile $templateParamFile
}