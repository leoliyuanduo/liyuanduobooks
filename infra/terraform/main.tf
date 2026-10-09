# Hosting: Azure Container Apps (serverless containers with managed ingress, TLS,
# revisions and scale-to-N), plus Log Analytics, where the app's structured JSON logs land.

data "azurerm_resource_group" "this" {
  name = var.resource_group_name
}

locals {
  tags = merge(
    {
      application = "bupa-books"
      managed-by  = "terraform"
    },
    var.tags,
  )
  use_private_registry = var.registry_server != ""
}

resource "azurerm_log_analytics_workspace" "this" {
  name                = "${var.name_prefix}-logs"
  location            = data.azurerm_resource_group.this.location
  resource_group_name = data.azurerm_resource_group.this.name
  sku                 = "PerGB2018"
  retention_in_days   = var.log_retention_days
  tags                = local.tags
}

resource "azurerm_container_app_environment" "this" {
  name                       = "${var.name_prefix}-env"
  location                   = data.azurerm_resource_group.this.location
  resource_group_name        = data.azurerm_resource_group.this.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.this.id
  tags                       = local.tags
}

resource "azurerm_container_app" "this" {
  name                         = "${var.name_prefix}-app"
  resource_group_name          = data.azurerm_resource_group.this.name
  container_app_environment_id = azurerm_container_app_environment.this.id
  revision_mode                = "Single"
  tags                         = local.tags

  dynamic "secret" {
    for_each = local.use_private_registry ? [1] : []
    content {
      name  = "registry-password"
      value = var.registry_password
    }
  }

  dynamic "registry" {
    for_each = local.use_private_registry ? [1] : []
    content {
      server               = var.registry_server
      username             = var.registry_username
      password_secret_name = "registry-password"
    }
  }

  ingress {
    external_enabled           = true
    target_port                = 8080
    transport                  = "auto"
    allow_insecure_connections = false # HTTPS only; TLS terminates at the managed ingress

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = var.min_replicas
    max_replicas = var.max_replicas

    container {
      name   = "app"
      image  = var.container_image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }

      # The app is only reachable through the Container Apps ingress, whose IPs are dynamic,
      # so the forwarded client IP is trusted (one hop). See ADR-0007.
      env {
        name  = "ForwardedHeaders__TrustAllProxies"
        value = "true"
      }

      startup_probe {
        transport = "HTTP"
        path      = "/health/live"
        port      = 8080
      }

      liveness_probe {
        transport = "HTTP"
        path      = "/health/live"
        port      = 8080
      }

      # Readiness does not depend on the upstream API (an upstream outage must not take
      # every replica out of rotation; the app degrades gracefully instead).
      readiness_probe {
        transport = "HTTP"
        path      = "/health/ready"
        port      = 8080
      }
    }

    http_scale_rule {
      name                = "http-concurrency"
      concurrent_requests = "50"
    }
  }
}
