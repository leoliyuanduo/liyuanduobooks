output "app_url" {
  description = "Public HTTPS URL of the app."
  value       = "https://${azurerm_container_app.this.ingress[0].fqdn}"
}

output "log_analytics_workspace_id" {
  description = "Where to query the app's structured logs (ContainerAppConsoleLogs_CL)."
  value       = azurerm_log_analytics_workspace.this.workspace_id
}
