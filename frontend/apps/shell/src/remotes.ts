import { loadRemote, registerRemotes } from "@module-federation/enhanced/runtime";
import type { ComponentType } from "react";
import type { RemoteAppProps } from "@november/shared";

export type RemoteName = "payments" | "approvals" | "incoming";

type RemoteModule = { default: ComponentType<RemoteAppProps> };

const remoteNames: RemoteName[] = ["payments", "approvals", "incoming"];

function registerAll(force: boolean) {
  registerRemotes(
    remoteNames.map((name) => ({
      name,
      entry: `${window.location.origin}/remotes/${name}/mf-manifest.json`,
    })),
    { force },
  );
}

let registered = false;

export async function loadRemoteApp(name: RemoteName, retry: boolean): Promise<RemoteModule> {
  if (!registered || retry) {
    registerAll(registered);
    registered = true;
  }
  const module = await loadRemote<RemoteModule>(`${name}/App`);
  if (!module) throw new Error(`The ${name} app did not expose ./App`);
  return module;
}
