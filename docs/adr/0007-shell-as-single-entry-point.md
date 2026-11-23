# ADR 0007: The shell as the single entry point

## Status

Accepted

## Context

The browser needs to reach four front-end bundles (shell, payments, approvals, incoming) and one API (the BFF). There are two obvious ways to expose them:

- **Each service public on its own host name**, for example `payments.example.com`, `api.example.com`. The shell then loads remotes cross-origin and calls the API cross-origin.
- **One public origin** that fronts everything.

Cross-origin has real costs here. Remotes served from another origin need CORS headers, and the module-federation runtime fetches manifests and chunks with `fetch` and script tags, so every host must be configured and kept in step. The BFF session is an `HttpOnly`, `SameSite=Strict` cookie ([ADR 0002](0002-backend-for-frontend.md)), and `SameSite=Strict` cookies are not sent on cross-site requests, so a separate API host would need a looser cookie policy. Each extra public host is also another certificate, another DNS record and another thing to put behind a WAF.

## Decision

The **shell is the only publicly reachable front-end service**. It is an nginx container that:

- serves the shell's static files, with an SPA fallback to `index.html`;
- reverse-proxies `/api/` to the BFF (`$BFF_UPSTREAM`);
- reverse-proxies `/remotes/payments/`, `/remotes/approvals/` and `/remotes/incoming/` to the matching remote containers (`$PAYMENTS_UPSTREAM`, `$APPROVALS_UPSTREAM`, `$INCOMING_UPSTREAM`), **stripping the prefix**;
- keeps nginx's default `Host` header (the upstream's host), because Azure Container Apps routes internal traffic by `Host`;
- sets `X-Forwarded-For`, `X-Forwarded-Proto` and `X-Forwarded-Host`.

The BFF and the remotes have **internal ingress only**. The shell loads each remote from the same-origin URL `/remotes/<name>/mf-manifest.json`, and the remotes' assets resolve relative to that manifest URL (`publicPath: "auto"`), so the same bundle works behind any prefix.

In development the shell's Rsbuild dev server plays the same role with its `server.proxy` setting (`/api` to `http://localhost:5080`, `/remotes/<name>` to `http://localhost:300x` with the prefix stripped). The browser-side code path is identical in dev and in containers.

Upstream addresses are resolved when a request arrives (a `resolver` plus `proxy_pass` with a variable), not when nginx starts. A remote that is down therefore produces a `502`/`504` for that remote's requests only, and the shell container still starts and serves everything else.

## Consequences

Good:

- No CORS anywhere. The cookie can stay `SameSite=Strict`, and the `X-CSRF: 1` header check is enough for CSRF protection.
- One public host name, one certificate, one place for edge policy (security headers, rate limits, a WAF in front).
- Remotes and the BFF are not reachable from the internet, which shrinks the attack surface.
- The same code path runs in dev, in Docker Compose and in Azure.

Costs we accept:

- **The shell is on the path of every request.** It is static files and a proxy, which is cheap and easy to scale, but if it is down the portal is down, even though the remotes are healthy.
- **An extra hop** for remote assets and API calls. Remote assets are content-hashed and cacheable (`Cache-Control: immutable`), and only the manifest and entry file are `no-cache`, so the hop is paid mostly for small files.
- **The shell's configuration knows the remotes' names.** Adding a remote means a new `location` block and a new environment variable in the shell's nginx template, as well as the code change in the shell.
- **Prefix stripping is a contract.** A remote must not assume it is served at `/`, which is why it uses `publicPath: "auto"` and a `basePath` prop instead of absolute paths.
- **Long-lived connections need care.** The setup has no WebSockets. If the BFF later adds server-sent events or sockets, the `/api/` location needs the usual upgrade headers and longer timeouts.
