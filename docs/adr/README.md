# Architecture decision records

An ADR captures one decision: the context, what was decided, and the consequences, including the costs. They are short, and they are not edited after the fact except to fix errors. If a decision changes, write a new ADR that supersedes the old one.

| ADR | Decision | In one line |
| --- | --- | --- |
| [0001](0001-micro-frontends-with-module-federation.md) | Micro frontends with Module Federation | A shell loads three separately built remotes at runtime from same-origin manifests, and shares four libraries as singletons. |
| [0002](0002-backend-for-frontend.md) | Backend for Frontend | One ASP.NET Core service shapes data for the UI, owns the session cookie and returns one error format. |
| [0003](0003-mediator-pattern.md) | Mediator pattern | Endpoints send requests through martinothamar/Mediator; handlers stay small and behaviors add logging and validation. |
| [0004](0004-vertical-slice-architecture.md) | Vertical slice architecture | Code is organised by feature, with one file per use case holding request, validator and handler. |
| [0005](0005-maker-checker-and-fx-requote.md) | Maker-checker and FX re-quote | The approver commits to a fresh quote; the downstream platform enforces the rules and the BFF orchestrates. |
| [0006](0006-in-memory-downstream-and-mock-auth.md) | In-memory downstream and mock auth | Fake platforms behind interfaces and persona login behind a seam, so the demo runs with nothing else installed. |
| [0007](0007-shell-as-single-entry-point.md) | The shell as the single entry point | One nginx serves the shell and proxies `/api/` and `/remotes/<name>/`, so the browser sees one origin and there is no CORS. |
| [0008](0008-azure-container-apps.md) | Azure Container Apps | Five containers in one Consumption environment, with only the shell exposed publicly. |
| [0009](0009-bicep-for-infrastructure-as-code.md) | Bicep for infrastructure as code | Small hand-written modules and one parameter file per environment, validated by compiling in CI. |
| [0010](0010-azure-pipelines-for-ci-cd.md) | Azure Pipelines for CI/CD | Azure Pipelines is the primary reference; GitHub Actions only keeps the public mirror green. |

Back to the [documentation index](../README.md).
