import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { defineConfig } from "@rsbuild/core";
import { pluginReact } from "@rsbuild/plugin-react";
import { pluginModuleFederation } from "@module-federation/rsbuild-plugin";

export const remotePorts = {
  payments: 3001,
  approvals: 3002,
  incoming: 3003,
} as const;

const singletons = ["react", "react-dom", "react-router", "@tanstack/react-query"];

function sharedSingletons() {
  const manifest = JSON.parse(readFileSync(resolve(process.cwd(), "package.json"), "utf8")) as {
    dependencies: Record<string, string>;
  };
  return Object.fromEntries(
    singletons.map((name) => [name, { singleton: true, requiredVersion: manifest.dependencies[name] }]),
  );
}

export function defineShell() {
  const bff = process.env.BFF_URL ?? "http://localhost:5080";
  const remoteProxies = Object.fromEntries(
    Object.entries(remotePorts).map(([name, port]) => [
      `/remotes/${name}`,
      { target: `http://localhost:${port}`, pathRewrite: { [`^/remotes/${name}`]: "" } },
    ]),
  );

  return defineConfig({
    plugins: [
      pluginReact(),
      pluginModuleFederation({ name: "shell", shared: sharedSingletons(), dts: false }),
    ],
    html: { title: "November Bank" },
    server: {
      port: 3000,
      strictPort: true,
      proxy: { "/api": bff, ...remoteProxies },
    },
  });
}

export function defineRemote(options: { name: keyof typeof remotePorts; title: string }) {
  const port = remotePorts[options.name];

  return defineConfig({
    plugins: [
      pluginReact(),
      pluginModuleFederation({
        name: options.name,
        filename: "remoteEntry.js",
        exposes: { "./App": "./src/App.tsx" },
        shared: sharedSingletons(),
        dts: false,
      }),
    ],
    html: {
      title: `November ${options.title}`,
      tags: [{ tag: "base", attrs: { href: "/" }, append: false }],
    },
    output: { assetPrefix: "auto" },
    dev: { assetPrefix: "auto", client: { port } },
    server: {
      port,
      strictPort: true,
      headers: { "Access-Control-Allow-Origin": "*" },
      proxy: { "/api": process.env.BFF_URL ?? "http://localhost:5080" },
    },
  });
}
