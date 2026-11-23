# ADR 0003: Mediator pattern

## Status

Accepted

## Context

The BFF has about twenty use cases. Each needs the same cross-cutting behavior: structured logging with timing, request validation and consistent error output. Calling handler classes directly from endpoints would repeat that wiring in every endpoint, or push it into base classes and filters that know nothing about the use case.

We also want to show a technique that is common in .NET codebases and teach it with its trade-offs, not just its benefits.

The best-known library, MediatR, moved to a commercial licence from version 13. For a public reference repository we want dependencies that anyone can clone and run without reading licence terms.

## Decision

We use the mediator pattern with [martinothamar/Mediator](https://github.com/martinothamar/Mediator) (`Mediator.Abstractions` and `Mediator.SourceGenerator`, version 3.x).

- Endpoints build a request record and call `ISender.Send`. They contain no logic.
- Requests implement `ICommand<T>` (changes state) or `IQuery<T>` (reads). Handlers implement `ICommandHandler` or `IQueryHandler` and return `ValueTask<T>`.
- Two pipeline behaviors, registered in this order (first is outermost): `LoggingBehavior`, then `ValidationBehavior` (FluentValidation, which throws, which becomes a 400 `ProblemDetails`).
- Notifications are demonstrated once: the approve handler publishes `PaymentApproved` and an audit-log handler reacts.
- Mediator is registered `Scoped`, because handlers depend on `ICurrentUser`, which is per request.
- Errors are exceptions (`DomainException`) rendered by one `IExceptionHandler`, so handlers stay linear.

The programming model is close to MediatR's, so what is learned here transfers. The mapping is in [`docs/bff/README.md`](../bff/README.md#mapping-to-mediatr).

## Consequences

Good:

- Cross-cutting behavior is written once and applies to every use case, including future ones.
- Endpoints are uniform and tiny. A use case is easy to test and easy to find.
- The source generator wires handlers at compile time: no startup reflection, and a missing handler is a build error rather than a runtime one.
- No licensing concern for readers who copy the code.

Costs:

- Indirection. Go-to-definition on `Send` does not reach the handler, and the call graph is no longer visible in the editor. We mitigate it with a strict convention: request, validator and handler live in one file named after the use case ([ADR 0004](0004-vertical-slice-architecture.md)).
- Handlers can be tempted to send other requests to each other, which creates hidden coupling. We do not do that; shared logic goes in plain classes (`PaymentViewBuilder`, `PaymentPolicy`).
- For a service this size the pattern is more ceremony than direct calls would need. It earns its place through the behaviors and as a teaching example, and a team should weigh it honestly.
- A less-common library than MediatR has a smaller community and fewer answers online.
- Using exceptions for expected failures is a trade-off. A `Result<T>` would fit the same structure.
