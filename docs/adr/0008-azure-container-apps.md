# 8. Host the system on Azure Container Apps

## Status

Accepted

## Context

November is five small, stateless HTTP containers: an nginx shell, an ASP.NET Core BFF, and three nginx-served micro frontends. They need to:

* run the same images locally and in the cloud
* expose exactly one public entry point and keep the rest private
* scale with load, and ideally to zero when idle, because the demo environment is used rarely
* pull images and read secrets without stored passwords
* require little operational effort, since the repository is educational and has no platform team

Candidates were Azure App Service for Containers, Azure Kubernetes Service, Azure Container Instances and Azure Container Apps.

## Decision

Deploy all five containers as **Azure Container Apps** in one Consumption-only environment.

* The shell uses external ingress. The BFF and the remotes use internal ingress and are reached by name from the shell's reverse proxy.
* Apps pull images from Azure Container Registry and read Key Vault secrets with a user-assigned managed identity.
* HTTP concurrency scale rules drive replica counts. Development scales to zero; production keeps a minimum of two replicas for the shell and BFF.
* Logs go to Log Analytics and telemetry to workspace-based Application Insights.

## Consequences

Positive:

* No cluster to run or upgrade. Envoy-based ingress, revisions, internal DNS and TLS are provided.
* Scale to zero keeps the dev environment close to free.
* Managed identity and Key Vault references remove every stored credential.
* The same container images run under Docker Compose with only environment variables changing.

Negative:

* Less control than Kubernetes: no custom controllers, limited networking knobs on the Consumption profile, and a platform that changes underneath you.
* Scale to zero adds a cold start to the first request.
* Moving to a private network later means recreating the environment with a subnet, since an existing environment cannot be moved into a virtual network.
* The platform is Azure-specific. Portability is retained at the container level, not at the deployment level.

If the system grew to dozens of services, custom networking or sidecar-heavy patterns, AKS would deserve a fresh look.
