# ADR 0004: Vertical slice architecture

## Status

Accepted

## Context

A common way to organise a .NET API is by technical layer: `Controllers/`, `Services/`, `Repositories/`, `Models/`. To change one feature you touch four folders, and every layer grows into a large bag of unrelated classes. In a BFF the "layers" are thin anyway (there is no data access, only calls to platforms), so the layering mostly adds indirection.

We want a structure where adding or changing a use case touches one obvious place, and where a newcomer can find the code for `POST /api/payments/{id}/approve` without searching.

## Decision

We organise by feature, in vertical slices.

- `Features/<Area>/` has one folder per area: Payments, Incoming, Dashboard, Session, Accounts, Beneficiaries, Fx, Reference.
- **One file per use case**, named after it (`ApprovePayment.cs`). The file holds the request record, its FluentValidation validator, its handler, and any response record used only by that use case.
- **One `<Area>Endpoints.cs` per area** maps the minimal API routes with `MapGroup` and `TypedResults`. Endpoints only bind HTTP, build the request and send it through the mediator.
- Code shared by several use cases in an area sits next to them with a descriptive name: `PaymentResponses.cs` (UI-shaped records), `PaymentViewBuilder.cs`, `PaymentPolicy.cs`.
- `Infrastructure/` holds what every slice uses: pipeline behaviors, authentication, error handling, validation helpers.
- `Downstream/` holds the interfaces for the bank platforms and their in-memory fakes. Slices depend on the interfaces, never on the fakes.

## Consequences

Good:

- Adding a use case is one new file plus, at most, one route line. The how-to in [`docs/bff/README.md`](../bff/README.md#5-adding-a-feature-step-by-step) has four steps.
- Changes stay local, so a feature is easier to review and to delete.
- The folder tree reads like the product: Payments, Incoming, Dashboard.

Costs:

- Some duplication is accepted on purpose. Two slices may each have a small validator or mapper that looks alike. We only extract shared code when it is clearly the same thing (for example `MoneyRules`, `BankFormats`).
- Slices can drift in style if nobody holds the convention. The file-per-use-case rule is the guard rail.
- Cross-slice reuse needs judgment. `GetDashboard` uses `PaymentViewBuilder` and `PaymentPolicy` from the Payments area. That is deliberate (the dashboard shows payments) but it is a dependency between slices, and we keep it one-directional.
- Developers used to layers must unlearn the habit of creating a service class "just in case". If a handler is a single downstream call, that is fine.
