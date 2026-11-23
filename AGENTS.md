# Working rules

Rules for anyone changing this repository, whether you are a person or a coding agent. They are short on purpose. Where a rule has a longer explanation, it links to it.

November is a reference repository for learning. Clear, small and correct beats clever and complete.

## Project map

| Path | What lives there |
| --- | --- |
| `bff/` | .NET 10 Backend-for-Frontend. `src/November.Bff.Api` and `tests/November.Bff.Tests`. |
| `frontend/` | npm workspaces: `apps/shell` (host), `apps/payments`, `apps/approvals`, `apps/incoming` (remotes), `packages/ui`, `packages/shared`. |
| `infra/` | Azure Bicep: `main.bicep`, `modules/`, one `.bicepparam` per environment. |
| `pipelines/` | Azure Pipelines (the primary CI/CD reference). |
| `.github/workflows/ci.yml` | Build-only mirror so the public GitHub repo shows a green build. |
| `docs/` | All documentation. Start at [`docs/README.md`](docs/README.md). |
| `docker-compose.yml` | The whole system on one machine. |

### Commands

Run these from the repository root unless noted.

| Task | Command |
| --- | --- |
| Run everything in containers | `docker compose up --build` then open http://localhost:3000 |
| Run the BFF | `dotnet run --project bff/src/November.Bff.Api` (http://localhost:5080, API reference at `/scalar`) |
| Run the front end | `cd frontend && npm ci && npm run dev` (http://localhost:3000) |
| Build and test the BFF | `cd bff && dotnet build November.Bff.slnx && dotnet test --solution November.Bff.slnx` |
| Typecheck, build and test the front end | `cd frontend && npm run typecheck && npm run build && npm run test` |
| Validate Bicep | `az bicep build --file infra/main.bicep` and `az bicep build-params --file infra/main.dev.bicepparam` |

Before you finish a change, run the commands for the part you touched. If you touched the contract, run both the BFF and the front end checks.

## Contract first

[`docs/api-contract.md`](docs/api-contract.md) is the agreement between the BFF and the micro frontends.

1. Change the contract document first.
2. Change the BFF response records and the front end types in `frontend/packages/shared/src/types.ts` in the same change.
3. Names, casing and enum values must match exactly. If the two sides disagree, the contract decides, and you fix the side that is wrong.
4. Update the relevant guide in `docs/` if behaviour changed.

## BFF conventions

Read [`docs/bff/README.md`](docs/bff/README.md) before a non-trivial change.

- **One file per use case** in `Features/<Area>/`, holding the request record, its FluentValidation validator and its handler. Shared helpers for an area sit beside them with a descriptive name.
- **Endpoints only send through the mediator.** An endpoint binds HTTP, builds the request and calls `ISender.Send`. No logic, no calls to downstream services.
- **Business rules live in the Downstream fakes**, not in handlers. Handlers orchestrate, pre-check to give friendly errors, and shape responses. See [ADR 0005](docs/adr/0005-maker-checker-and-fx-requote.md).
- **Errors are `ProblemDetails` with a `code`.** Throw `DomainException` through the helpers in `Errors`. Add the new code to the table in the contract.
- **Time comes from `TimeProvider`.** Never call `DateTime.UtcNow` or `DateTimeOffset.UtcNow`.
- **Use martinothamar/Mediator, not MediatR.** The programming model is the same, but MediatR is not a dependency and must not become one.
- Slices depend on the interfaces in `Downstream/`, never on the fakes.
- Register new downstream fakes in `AddInMemoryDownstream`.

## Front end conventions

Read [`docs/frontend/README.md`](docs/frontend/README.md) before a non-trivial change.

- **Remotes never import each other.** They communicate through props from the shell, `CustomEvent`s on `window`, URLs and the BFF.
- **Shared code comes only from `@november/shared` and `@november/ui`.** Do not reach into another app's `src`.
- **All HTTP goes through `apiFetch`** (and the query hooks built on it). Do not call `fetch` directly in a component.
- **`react`, `react-dom`, `react-router` and `@tanstack/react-query` are singletons.** Keep their version ranges identical in every app's `package.json`.
- **URLs are same-origin and relative** (`/api/...`, `/remotes/<name>/...`). Never hard-code a host or port.
- Remotes must also run standalone, so keep them free of assumptions about the shell beyond the `{ basePath, session }` props.

## Infrastructure conventions

Read [`docs/infra/README.md`](docs/infra/README.md) before a non-trivial change.

- One Bicep module per concern, in `infra/modules/`, wired together in `main.bicep`.
- Environments differ only in `.bicepparam` files.
- **No secrets** in templates, parameter files, pipelines or images. Containers authenticate with a user-assigned managed identity and read secrets from Key Vault.
- Every parameter has a `@description`. That replaces inline comments.
- Only the shell has external ingress. The BFF and the remotes stay internal.
- Pipelines are references that are never executed from this repository. Keep them valid YAML and keep them in step with `docs/infra/ci-cd.md`.

## Code style

- **No inline comments.** No `//`, `/* */` or `#` explanations in code, Bicep, YAML, Dockerfiles or nginx configuration. Good names and tests carry the meaning.
- Allowed: XML doc comments on public APIs where the codebase already uses them, and a rare single-line note when the reason for something is genuinely non-obvious and a reader would otherwise misread it. The bar is high.
- Never add step labels, restatements, section banners or TODOs.

## Tests

- No test-driven development. Write a few critical tests, not many.
- The BFF has three tests that drive the real HTTP pipeline. The front end has a few unit tests for the FX countdown. Add to them only when a change would otherwise be unprotected in a way that matters (money, approvals, expiry).
- Do not add snapshot tests or tests that mirror the implementation.

## Documentation

- Keep the docs in step with the code in the same change. A doc that is wrong is worse than a missing one.
- Write plain, direct English. Prefer short sentences. Use a Mermaid diagram when a picture shows the flow better than a paragraph.
- Relative links must resolve. When you move or rename a file, search for links to it.
- The decisions behind the design are in [`docs/adr/`](docs/adr/README.md). Add an ADR when you change one of them.

## Content rules

- **Everything is fictional.** The bank, the company, the people, the rates and the data are invented. Never name, imitate or allude to a real bank, client, company or person.
- **No secrets and no real identifiers.** No keys, tokens, connection strings, subscription or tenant ids, real domain names or real account numbers. Use obvious placeholders and `.example` domains.
- Do not describe how the repository was authored, and do not credit tools in code, docs, commits or pull requests.

## Commits and pull requests

- Small, focused commits. One idea per commit.
- Write the subject line in the imperative mood and keep it short: `Add fixedSide to payment detail`.
- Say why in the body when it is not obvious.
- No tool attribution lines, no co-author trailers and no generated-by footers.
- Make sure the build and tests for the part you touched pass before you push.
