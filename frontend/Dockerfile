ARG NODE_VERSION=24

FROM node:${NODE_VERSION}-alpine AS deps
ARG APP
WORKDIR /app
COPY package.json package-lock.json ./
COPY packages/ui/package.json packages/ui/
COPY packages/shared/package.json packages/shared/
COPY apps/shell/package.json apps/shell/
COPY apps/payments/package.json apps/payments/
COPY apps/approvals/package.json apps/approvals/
COPY apps/incoming/package.json apps/incoming/
RUN npm ci --workspace=apps/${APP} --include-workspace-root --ignore-scripts

FROM deps AS build
ARG APP
COPY . .
RUN npm run build --workspace=apps/${APP}

FROM nginxinc/nginx-unprivileged:stable-alpine AS runtime
ARG APP
USER root
COPY nginx/snippets/ /etc/nginx/snippets/
COPY nginx/shell.conf.template nginx/remote.conf.template /tmp/nginx-templates/
RUN if [ "${APP}" = "shell" ]; then TEMPLATE=shell; else TEMPLATE=remote; fi \
    && mkdir -p /etc/nginx/templates \
    && cp /tmp/nginx-templates/${TEMPLATE}.conf.template /etc/nginx/templates/default.conf.template \
    && rm -rf /tmp/nginx-templates
USER nginx

ENV BFF_UPSTREAM=http://bff:8080 \
    PAYMENTS_UPSTREAM=http://payments:8080 \
    APPROVALS_UPSTREAM=http://approvals:8080 \
    INCOMING_UPSTREAM=http://incoming:8080 \
    NGINX_ENTRYPOINT_LOCAL_RESOLVERS=1 \
    NGINX_ENVSUBST_FILTER="^(BFF|PAYMENTS|APPROVALS|INCOMING)_UPSTREAM\$|^NGINX_LOCAL_RESOLVERS\$"

COPY --from=build /app/apps/${APP}/dist /usr/share/nginx/html

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s CMD wget -q -O /dev/null http://127.0.0.1:8080/healthz || exit 1
