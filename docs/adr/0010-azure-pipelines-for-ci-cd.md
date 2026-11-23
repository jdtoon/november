# 10. Use Azure Pipelines as the primary CI/CD reference

## Status

Accepted

## Context

November teaches an architecture that, in its real-world counterpart, is built and released through Azure DevOps. Readers who work in that environment need a pipeline reference that matches the tools they use: Azure Repos, Azure Pipelines, environments with approvals, and service connections.

The repository is also published on GitHub. A public repository with no visible build gives readers no evidence that the code compiles and the tests pass. GitHub Actions provides that evidence at no cost.

Deployment needs an Azure subscription, which this repository does not have, so no pipeline here can be executed end to end.

## Decision

* **Azure Pipelines is the primary reference**, in `pipelines/`: `ci.yml` for validation, `deploy.yml` for multi-stage delivery (build and push images, deploy dev, deploy prod), with shared templates and per-environment variable files.
* Deployment authenticates with an Azure Resource Manager **service connection that uses workload identity federation**, so there is no stored secret.
* Production is protected by an **Azure DevOps environment** with approvals and checks, and by branch policies with build validation on `main`.
* **GitHub Actions keeps one workflow**, `.github/workflows/ci.yml`, whose only purpose is to show a green build on the public copy. It has no deployment job.
* Dependabot stays enabled for the public mirror.
* The pipelines are documented as **unexecuted references** and are validated for YAML syntax only.

## Consequences

Positive:

* The reference matches the toolchain of the system it teaches.
* Stages, environments and templates demonstrate patterns that have no direct GitHub equivalent, and `docs/infra/ci-cd.md` maps the concepts between the two products.
* The public repository still has a real, running build.

Negative:

* The same checks are defined twice (`pipelines/ci.yml` and `.github/workflows/ci.yml`) and can drift. They are kept deliberately small to limit this.
* The Azure Pipelines YAML cannot be tested without an organization. Mistakes in task inputs or output-variable syntax may only appear on first run.
* Azure Repos has no built-in equivalent of Dependabot, so dependency update pull requests there would need Renovate or a similar tool.
