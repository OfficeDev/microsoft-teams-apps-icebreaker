output "manifest_path" {
  value = data.archive_file.app_package.output_path
}

output "app_service_name" {
  value = jsondecode(azurerm_resource_group_template_deployment.icebreaker.output_content).appServiceName.value
}
