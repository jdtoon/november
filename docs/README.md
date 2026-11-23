# Documentation

November is a fictional business-banking portal, built to teach how React micro frontends, a .NET Backend-for-Frontend and Azure Container Apps fit together. This folder explains the system, the decisions behind it and how it was put together.

## Suggested reading order

1. [`domain.md`](domain.md). The business primer: payments, FX quotes, maker-checker. Read this first if banking terms are new to you.
2. [`architecture.md`](architecture.md). The whole system on two diagrams, a request traced end to end, the security model and the seams where real systems would plug in.
3. [`api-contract.md`](api-contract.md). The agreement between the front end and the BFF. Keep it open while you read the code.
4. [`bff/README.md`](bff/README.md). The BFF guide: layout, request lifecycle, the mediator, authentication, errors, FX and four-eyes.
5. [`frontend/README.md`](frontend/README.md). The front end guide: Module Federation, shared singletons, the shell and remote contract, the payment wizard.
6. [`infra/README.md`](infra/README.md). The Bicep modules, identity, networking and how you would deploy.
7. [`infra/docker.md`](infra/docker.md). The container images and Docker Compose.
8. [`infra/ci-cd.md`](infra/ci-cd.md). The Azure Pipelines definitions and how they compare with GitHub Actions.
9. [`adr/README.md`](adr/README.md). The ten decisions, each with its trade-offs.
10. [`how-it-was-built.md`](how-it-was-built.md). The build order, the commands used and the problems met on the way.

## What is where

| Document | Answers |
| --- | --- |
| [`domain.md`](domain.md) | What does this app do, and what do the banking terms mean? |
| [`architecture.md`](architecture.md) | How do the pieces fit, and what would change in production? |
| [`api-contract.md`](api-contract.md) | What exactly do the endpoints accept and return? |
| [`bff/README.md`](bff/README.md) | How is the BFF built, and how do I add a feature? |
| [`frontend/README.md`](frontend/README.md) | How do the micro frontends work, and how do I add one? |
| [`infra/README.md`](infra/README.md) | What does the Azure infrastructure create? |
| [`infra/docker.md`](infra/docker.md) | How are the images built and run locally? |
| [`infra/ci-cd.md`](infra/ci-cd.md) | How does CI/CD work? |
| [`adr/README.md`](adr/README.md) | Why was it designed this way? |
| [`how-it-was-built.md`](how-it-was-built.md) | In what order was it built, and what went wrong? |

Everything in this repository is fictional. The root [`README.md`](../README.md) has the quickstart, and [`AGENTS.md`](../AGENTS.md) has the working rules for contributors.
