# 9. Define infrastructure with Bicep and hand-written modules

## Status

Accepted

## Context

Azure resources must be reproducible, reviewable and consistent between dev and prod. Clicking in the portal fails all three. The main options for infrastructure as code on Azure are:

* **ARM templates** in JSON. Native, but verbose and hard to read or review.
* **Bicep**. A first-party language that compiles to ARM, has day-zero support for new resource types and API versions, and needs no state file.
* **Terraform or OpenTofu**. Multi-cloud and mature, but needs state storage and lags new Azure features by a little.
* **Pulumi**. General-purpose languages, with a similar state story to Terraform.

For modules, Bicep also offers **Azure Verified Modules (AVM)**, which Microsoft maintains and publishes to a public registry.

## Decision

Use **Bicep**, composed from small **hand-written modules** in `infra/modules/`, one per concern. Environments differ only by `.bicepparam` files. Validation is `az bicep build` and `az bicep build-params`, run in CI, so a template that does not compile cannot merge. Every parameter carries a `@description`, in place of inline comments.

AVM is the recommendation for production work. Hand-written modules are used here because this repository is a learning resource: each module is short enough to read in one sitting, and it shows exactly which properties matter (admin user off, RBAC on, purge protection as a parameter) without asking the reader to understand a large third-party interface first.

## Consequences

Positive:

* Azure-native, no extra tooling or state, and the language is easy to read in a pull request.
* `what-if` previews every change before it is applied.
* Parameter files make the dev and prod differences explicit and reviewable.
* The modules teach the underlying resource model directly.

Negative:

* Hand-written modules do not carry the accumulated hardening of AVM (diagnostic settings, locks, private endpoint wiring) and must be maintained by us. The production hardening checklist in `docs/infra/README.md` lists what is missing.
* Bicep is Azure-only. A multi-cloud estate would need another tool.
* Two-phase first deployments (`deployApps`) are a workaround for a registry that must exist before the apps that pull from it. Terraform and Bicep share this problem, and neither solves it on its own.
