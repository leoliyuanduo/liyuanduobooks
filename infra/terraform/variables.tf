variable "resource_group_name" {
  description = "Existing resource group to deploy into. Created once by an administrator, so the deployment identity only needs Contributor on this group, not the subscription."
  type        = string
}

variable "name_prefix" {
  description = "Prefix for resource names, e.g. \"bupabooks-prod\"."
  type        = string

  validation {
    condition     = can(regex("^[a-z][a-z0-9-]{2,22}$", var.name_prefix))
    error_message = "Use 3-23 lowercase letters, digits or hyphens, starting with a letter."
  }
}

variable "container_image" {
  description = "Immutable image reference to run, e.g. ghcr.io/owner/repo:sha-<commit>. Never \"latest\"."
  type        = string

  validation {
    condition     = !endswith(var.container_image, ":latest")
    error_message = "Deploy an immutable tag (sha-<commit>), not :latest."
  }
}

variable "registry_server" {
  description = "Registry host for a private image (e.g. ghcr.io). Leave empty for a public image."
  type        = string
  default     = ""
}

variable "registry_username" {
  description = "Registry user for a private image."
  type        = string
  default     = ""
}

variable "registry_password" {
  description = "Registry token for a private image (a read-only package token). Supplied via TF_VAR_registry_password, never committed."
  type        = string
  default     = ""
  sensitive   = true
}

variable "min_replicas" {
  description = "Minimum replicas. 1 avoids cold starts for a user-facing app."
  type        = number
  default     = 1
}

variable "max_replicas" {
  description = "Maximum replicas under load."
  type        = number
  default     = 3
}

variable "log_retention_days" {
  description = "Log Analytics retention."
  type        = number
  default     = 30
}

variable "tags" {
  description = "Extra tags merged onto every resource."
  type        = map(string)
  default     = {}
}
