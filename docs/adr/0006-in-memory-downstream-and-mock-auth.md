# ADR 0006: In-memory downstream and mock auth

## Status

Accepted

## Context

November is a public, educational repository. Readers should be able to clone it and run the whole system with one command and no accounts, databases, identity provider or bank sandbox. At the same time the architecture it teaches (a BFF in front of platform services, behind a real identity provider) should be visible in the code, not hidden by shortcuts.

Two things would normally be external: the bank's backend platforms and the identity provider.

## Decision

**Downstream platforms are interfaces with in-memory fakes.**

- The BFF depends on interfaces (`IAccountsService`, `IBeneficiariesService`, `IFxService`, `IPaymentsService`, `IIncomingPaymentsService`, `IReferenceDataService`) in `Downstream/`.
- Singleton, thread-safe fakes in `Downstream/Fakes/` implement them with seeded fictional data, and own the business rules (see [ADR 0005](0005-maker-checker-and-fx-requote.md)).
- A hosted service simulates processing: after `Simulation:ProcessingDelaySeconds` (default 8) payments move from `Processing` to `Completed` and incoming payments to `Credited`.
- All fakes are registered in one method, `AddInMemoryDownstream`. That method is the seam where typed `HttpClient`s with resilience would plug in.
- State resets when the process restarts.

**Authentication is mocked, behind a seam.**

- `Auth:Mode=Mock` (the default, and on in every environment for this demo) exposes `GET /api/session/personas` and `POST /api/session/login`. The login screen lists three personas (Alex, Sam, Jordan) and signing in issues the normal session cookie.
- Everything after sign-in reads claims through `ICurrentUser`: user id, name, roles, company. Handlers and policies never know the claims came from a persona.
- `Auth:Mode=Oidc` is reserved for an Entra ID implementation. It throws a clear message at startup today. The design for it is in [`docs/bff/README.md`](../bff/README.md#swapping-mock-auth-for-entra-id).

## Consequences

Good:

- One command runs everything. Tests are fast and deterministic (the fakes use `TimeProvider`).
- The structure of a real integration is on display: interfaces, an orchestration layer, rules owned by the platform.
- Persona login makes the maker-checker journey easy to demonstrate, because one person can switch between Alex, Sam and Jordan.

Costs and risks:

- **Mock authentication is not security.** Anyone who can reach a deployed instance can sign in as any persona. This is acceptable only because every user, company, rate and payment in the repository is fictional. Do not deploy this mode in front of real data or reuse the pattern outside a demo.
- Data is lost on restart and cannot be shared between instances, so the BFF must run as a single replica.
- The fakes are simpler than real platforms (no latency, no partial failure, no idempotency, no pagination), so the BFF code does not exercise the failure handling a real integration needs. The resilience guidance in the BFF guide is therefore described, not demonstrated.
- The fakes talk to each other through internal interfaces (`IAccountLedger`, `IFxQuoteRedeemer`) that a real BFF would never see. They must be deleted with the fakes, not carried over.
- A reader could mistake the fakes' rules for BFF logic. The folder boundary (`Downstream/` versus `Features/`) and the docs are what prevent that.
