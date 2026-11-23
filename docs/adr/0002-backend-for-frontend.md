# ADR 0002: Backend for Frontend

## Status

Accepted

## Context

November is a set of React micro frontends (shell, payments, approvals, incoming) that need data from several bank platforms: accounts, beneficiaries, payments, FX dealing, reference data. Those platforms have APIs shaped around their own domains, not around screens.

Without an intermediate layer each micro frontend would:

- call several platforms and join the results in the browser (chatty, slow on mobile, duplicated across four apps);
- need to know where each platform lives and how it is secured;
- hold access tokens in browser storage, where an XSS bug can read them;
- need cross-origin access (CORS) to each platform.

The four apps are deployed independently, but the user experiences one product with one sign-in.

## Decision

We add one server-side service, a Backend-for-Frontend, written in ASP.NET Core on .NET 10.

- The shell is the only public entry point. Its nginx reverse-proxies `/api/` to the BFF, so the browser only ever talks to its own origin and no CORS is needed.
- The BFF exposes UI-shaped endpoints (for example `GET /api/dashboard` aggregates three platforms, payments carry `canApprove` and `canCancel`) and returns one error format (RFC 9457 `ProblemDetails` with a `code`).
- The session is an HttpOnly, `SameSite=Strict` cookie issued by the BFF. The browser never holds a token. Non-GET requests must carry `X-CSRF: 1`.
- The BFF is thin. It orchestrates calls, enforces authorization, pre-checks rules to give friendly errors and shapes responses. Business rules stay in the downstream platforms (see [ADR 0005](0005-maker-checker-and-fx-requote.md)).
- One BFF serves all four micro frontends, because they share a single user journey and a single contract ([`docs/api-contract.md`](../api-contract.md)).

## Consequences

Good:

- The browser makes one call per screen instead of several, and never learns the platform topology.
- Credentials for downstream platforms live on the server.
- Authorization, error shape and CSRF protection are implemented once.
- The micro frontends depend on one contract, which can be mocked or versioned.

Costs and risks:

- The BFF is a single deployable that every micro frontend depends on. A bug or outage there affects all of them, and it can become a coordination bottleneck between teams.
- It is one more network hop and one more thing to operate.
- It is tempting to put business rules in it because it is convenient. That makes it a second, hidden backend. The discipline is to keep rules in the platforms and only orchestration here.
- With the cookie model the BFF holds session state, so it needs sticky or shared session handling once it scales beyond one instance. In this repo that is moot because all data is in memory ([ADR 0006](0006-in-memory-downstream-and-mock-auth.md)).
- If different clients (a mobile app, a partner API) arrive with different needs, the usual answer is another BFF per client rather than a bigger one.

See [`docs/bff/README.md`](../bff/README.md) for the implementation.
