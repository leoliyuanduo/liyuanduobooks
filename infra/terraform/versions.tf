terraform {
  required_version = ">= 1.9.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Remote state in Azure Storage, authenticated with Entra ID (no storage keys).
  # Values are supplied at init time:  terraform init -backend-config=backend.hcl
  backend "azurerm" {}
}

provider "azurerm" {
  features {}
  # In CI, authentication is GitHub OIDC (ARM_USE_OIDC, ARM_CLIENT_ID, ARM_TENANT_ID,
  # ARM_SUBSCRIPTION_ID): no client secret exists anywhere.
  storage_use_azuread = true
}
