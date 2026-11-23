# November front end

React micro frontends for the November business-banking demo: a **shell** (host) and three Module Federation **remotes**, **payments**, **approvals** and **incoming**, sharing a small design system and an API client.

| App | Dev port | Role |
| --- | --- | --- |
| `apps/shell` | 3000 | Layout, sign-in, dashboard, loads the remotes |
| `apps/payments` | 3001 | Payments hub, three payment wizards, payment detail |
| `apps/approvals` | 3002 | Pending approvals, approve with a fresh FX quote, reject |
| `apps/incoming` | 3003 | Incoming payments and the respond form |
| `packages/ui` | | `@november/ui`: tokens and accessible components |
| `packages/shared` | | `@november/shared`: contract types, API client, events, hooks |

```bash
npm ci
npm run dev          # http://localhost:3000, expects the BFF on http://localhost:5080
npm run typecheck
npm run build
npm run test
```

Requires Node 24 (`.nvmrc`). Container images are built with one Dockerfile and `--build-arg APP=shell|payments|approvals|incoming`.

The full guide, including how Module Federation is wired, the shell and remote contract, how to add a new micro frontend, and the Docker and nginx setup, is in [`docs/frontend/README.md`](../docs/frontend/README.md).
