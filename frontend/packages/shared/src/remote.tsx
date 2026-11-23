import { createContext, useContext, useMemo } from "react";
import type { ReactNode } from "react";
import type { Role, Session } from "./types";

export type RemoteAppProps = {
  basePath: string;
  session: Session;
};

type RemoteContextValue = {
  basePath: string;
  session: Session;
  href: (path?: string) => string;
  hasRole: (role: Role) => boolean;
};

const RemoteContext = createContext<RemoteContextValue | null>(null);

export function RemoteProvider({ basePath, session, children }: RemoteAppProps & { children: ReactNode }) {
  const value = useMemo<RemoteContextValue>(() => {
    const root = basePath.replace(/\/+$/, "");
    return {
      basePath: root,
      session,
      href: (path = "") => `${root}/${path.replace(/^\/+/, "")}`.replace(/\/$/, "") || "/",
      hasRole: (role) => session.roles.includes(role),
    };
  }, [basePath, session]);

  return <RemoteContext value={value}>{children}</RemoteContext>;
}

export function useRemote(): RemoteContextValue {
  const value = useContext(RemoteContext);
  if (!value) throw new Error("useRemote must be used inside a RemoteProvider");
  return value;
}
