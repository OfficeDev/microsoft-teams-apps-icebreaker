provider "azurerm" {
  features {
  }
}

data "azurerm_client_config" "current" {}

resource "azuread_application" "icebreaker" {
  display_name     = "${var.name}-${var.stage}-${var.suffix}"
  owners           = [data.azurerm_client_config.current.object_id]
  sign_in_audience = "AzureADMultipleOrgs"
}

resource "azuread_application_password" "icebreaker" {
  application_object_id = azuread_application.icebreaker.object_id
}

locals {
  arm_parameters = {
    botAppID             = azuread_application.icebreaker.application_id
    botAppPassword       = azuread_application_password.icebreaker.value
    appName              = var.name
    appDescription       = var.description_long
    DefaultCulture       = var.defaultCulture
    pairingWeekInterval  = var.pairingWeekInterval
    pairingDayOfWeek     = var.pairingDayOfWeek
    pairingHour          = var.pairingHour
    pairingTimeZone      = var.pairingTimeZone
    sku                  = var.sku
    cosmosEnableFreeTier = var.cosmosEnableFreeTier
  }
}

data "azurerm_resource_group" "icebreaker" {
  name = "rg-${var.name}-${var.stage}-${var.suffix}"
}

resource "azurerm_resource_group_template_deployment" "icebreaker" {
  name                = "icebreaker-deployment"
  deployment_mode     = "Complete"
  resource_group_name = data.azurerm_resource_group.icebreaker.name
  template_content    = file("../azuredeploy.json")
  parameters_content = jsonencode(
    { for parameter, value in local.arm_parameters : parameter => {
      value = value
    } }
  )
}
