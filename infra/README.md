# Infrastructure: Azure Container Apps

`infra/terraform` deploys the single app image (BFF + React build) to **Azure Container Apps** in `australiaeast`, with logs in **Log Analytics**. `.github/workflows/deploy.yml` applies it after every green build of `main`, behind a manual approval.

> **Status:** validated in CI (`terraform fmt`, `validate`, `tflint`, `checkov` in `infra-ci.yml`). It has not been applied to a subscription as part of this submission. The steps below are the one-off setup needed to turn deployment on.

## What gets created

| Resource                   | Purpose                                                                                                           |
| -------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Log Analytics workspace    | Stores the app's structured JSON logs and Container Apps system logs (30-day retention)                           |
| Container Apps environment | Managed hosting boundary, wired to the workspace                                                                  |
| Container app              | The image, HTTPS-only managed ingress, startup/liveness/readiness probes, 1–3 replicas scaled on HTTP concurrency |

## Design choices

- **Least privilege.** The resource group is created once by an administrator. The deploy identity gets _Contributor on that group only_, not on the subscription.
- **No secrets in GitHub.** GitHub Actions authenticates to Azure with **OIDC workload identity federation**. Terraform state lives in Azure Storage, accessed with Entra ID (no storage keys).
- **Immutable releases.** Deployments reference `ghcr.io/<repo>:sha-<commit>`; Terraform rejects `:latest`. Rolling back means redeploying an older SHA (`workflow_dispatch` with `image_tag`).
- **Readiness does not depend on Bupa.** An upstream outage degrades the app (stale data) instead of taking every replica out of rotation.
- **Forwarded headers.** The app is only reachable through the Container Apps ingress, so `ForwardedHeaders__TrustAllProxies=true` lets the per-IP rate limit see real client IPs (ADR-0007).

## One-off setup

Run as a subscription administrator (Azure CLI):

```bash
LOCATION=australiaeast
APP_RG=rg-bupabooks-prod
STATE_RG=rg-tfstate
STATE_SA=sttfstatebupabooks$RANDOM   # globally unique
REPO=<github-owner>/<repo>

# 1. Resource groups for the app and for Terraform state
az group create -n $APP_RG -l $LOCATION
az group create -n $STATE_RG -l $LOCATION

# 2. State storage: Entra ID auth only, no shared keys, no public blob access
az storage account create -n $STATE_SA -g $STATE_RG -l $LOCATION --sku Standard_LRS \
  --min-tls-version TLS1_2 --allow-blob-public-access false --allow-shared-key-access false
az storage container create -n tfstate --account-name $STATE_SA --auth-mode login

# 3. Deploy identity, trusted only for this repo's "production" environment
APP_ID=$(az ad app create --display-name "github-$REPO-deploy" --query appId -o tsv)
az ad sp create --id $APP_ID
az ad app federated-credential create --id $APP_ID --parameters "{
  \"name\": \"github-production\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$REPO:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}"

# 4. Least-privilege role assignments
az role assignment create --assignee $APP_ID --role Contributor \
  --scope $(az group show -n $APP_RG --query id -o tsv)
az role assignment create --assignee $APP_ID --role "Storage Blob Data Contributor" \
  --scope $(az storage account show -n $STATE_SA -g $STATE_RG --query id -o tsv)
```

Then in GitHub (**Settings → Environments → New environment `production`**):

- Add **required reviewers** (the approval gate).
- Add these **variables**: `AZURE_CLIENT_ID` (`$APP_ID`), `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_RESOURCE_GROUP` (`$APP_RG`), `NAME_PREFIX` (e.g. `bupabooks-prod`), `TFSTATE_RESOURCE_GROUP` (`$STATE_RG`), `TFSTATE_STORAGE_ACCOUNT` (`$STATE_SA`).
- Make the GHCR package public (**Packages → bupa-books → Package settings**), or add a `GHCR_READ_TOKEN` secret (a fine-grained token with `read:packages`).

The next green `Container` run on `main` deploys automatically, after approval.

## Running Terraform locally

```bash
cd infra/terraform
cp backend.hcl.example backend.hcl        # fill in, git-ignored
cp prod.tfvars.example prod.tfvars        # fill in, git-ignored
az login
terraform init -backend-config=backend.hcl
terraform plan -var-file=prod.tfvars -var="container_image=ghcr.io/<repo>:sha-<commit>"
```

Commit the generated `.terraform.lock.hcl` after the first `init`, so every run uses the same provider builds.

## Scaling beyond one replica

The per-IP rate limiter, the response cache and the last-known-good fallback are per replica. With more replicas, move rate limiting to a gateway (Azure Front Door or API Management) and put Azure Cache for Redis behind `HybridCache` (one line in `AddInfrastructure`).
