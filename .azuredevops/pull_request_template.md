## What and why

Describe the change and the reason for it in a few sentences.

Linked work items: AB#

## Checklist

- [ ] The pull request build is green (BFF, frontend, infra and container jobs)
- [ ] BFF tests added or updated for behaviour changes
- [ ] `docs/api-contract.md` updated if an endpoint or error code changed
- [ ] No secrets, keys or real identifiers in code, parameter files or pipeline variables
- [ ] Bicep changes reviewed with `az deployment group what-if` output attached
- [ ] Docs updated where the change affects how someone runs or deploys the system

## How to verify

List the steps a reviewer can follow, for example `docker compose up --build` and a page to open at http://localhost:3000.
