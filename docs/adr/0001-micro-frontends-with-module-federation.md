# ADR 0001: Micro frontends with Module Federation

## Status

Accepted

## Context

November is a business-banking portal with journeys that differ a lot in shape and in who would own them in a real bank: making payments (long, multi-step forms), approvals (a checker's queue and decisions) and incoming payments (short respond-to-the-bank flows). In a bank these would be separate teams on separate release cycles, and a change to one journey should not force a redeploy or a regression test of the others.

At the same time the portal must feel like one product: one sign-in, one header and navigation, one visual language, one URL space.

We considered four ways to build the front end:

1. **One SPA with route-based code splitting.** Simplest, and the right answer for most products. Every journey still ships in one build, one pipeline and one deployable. It does not demonstrate independent delivery.
2. **iframes per journey.** Strong isolation, but poor accessibility and responsive behaviour, awkward focus and history handling, and no shared in-page state.
3. **Build-time composition (journeys as npm packages).** Shared code is easy, but every release of a journey needs a rebuild and redeploy of the host, so the deploys are not independent.
4. **Runtime composition with Module Federation.** A host loads separately built and deployed bundles at runtime and shares chosen libraries with them.

## Decision

We use runtime composition with **Module Federation 2.0** (Rsbuild with `@module-federation/rsbuild-plugin`).

- A **shell** (host) owns the layout, header, navigation, sign-in, the dashboard, the QueryClient and the router. It contains no journey code.
- Three **remotes**, `payments`, `approvals` and `incoming`, each expose a single module, `./App`, a React component that takes `{ basePath, session }`.
- Remotes are not declared at build time. The shell registers them at runtime from same-origin manifest URLs (`/remotes/<name>/mf-manifest.json`) and loads them with `React.lazy`. Deploying a remote publishes new files behind a stable manifest URL and needs no shell release.
- `react`, `react-dom`, `react-router` and `@tanstack/react-query` are **singletons** shared between host and remotes, with `requiredVersion` taken from each app's `package.json`. Everything else, including the two workspace packages `@november/ui` and `@november/shared`, is bundled into each app.
- Remotes never import each other. They talk to the shell and to each other only through three things: the props the shell passes, a small set of `CustomEvent`s on `window`, and URLs.
- Each remote can also run **standalone** (`StandaloneHost`) so a team can develop it without the shell.
- Each remote is wrapped by the shell in a `Suspense` and an error boundary, so a remote that is down degrades to a friendly message while the rest of the portal keeps working.

See [`docs/frontend/README.md`](../frontend/README.md) for how this is wired.

## Consequences

Good:

- The three journeys build, test and deploy independently. A remote's release is "publish its static files", and the shell picks it up on the next load.
- The failure of one remote does not take the portal down.
- Teams can work in their own workspace with their own dependencies, and the standalone mode shortens the feedback loop.

Costs we accept:

- **More moving parts.** Four builds, four images, a runtime contract and a proxy topology, for what is a modest UI. For a single team this is usually not worth it. This repository does it on purpose, to teach it.
- **Version coupling hidden in the singletons.** The four shared libraries must stay compatible across apps. We keep them on identical ranges and treat a major upgrade as a coordinated change. A mismatch is not a build error, it is a runtime warning and a fallback, so it must be caught by review and by running the apps together.
- **Duplicated code.** The design system and shared client are bundled four times. The bundles are small, and the alternative (making them federated singletons) would add a second versioning problem.
- **No shared state except through the contract.** Anything one remote needs from another has to be expressed as an event, a URL or the BFF. That is the point, but it takes discipline.
- **Cross-remote UX consistency is a process problem.** The shared `@november/ui` package helps, but nothing stops a remote drifting from it. Visual review and a single token file are the guard rails.
- **Debugging crosses bundle boundaries.** Errors loading a remote surface as runtime errors from the federation runtime, not as build errors.
