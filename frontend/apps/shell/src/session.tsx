import { createContext, useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import { Navigate, Outlet, useLocation } from "react-router";
import { Alert, Button, Spinner } from "@november/ui";
import { queries } from "@november/shared";
import type { Session } from "@november/shared";
import styles from "./session.module.css";

const SessionContext = createContext<Session | null>(null);

export function useSession(): Session {
  const session = useContext(SessionContext);
  if (!session) throw new Error("useSession must be used under RequireSession");
  return session;
}

export function RequireSession() {
  const location = useLocation();
  const session = useQuery(queries.session());

  if (session.isPending) {
    return (
      <div className={styles.center}>
        <Spinner label="Checking your session" />
      </div>
    );
  }

  if (session.isError) {
    return (
      <div className={styles.center}>
        <Alert
          tone="danger"
          title="November is unavailable"
          actions={
            <Button variant="secondary" size="sm" onClick={() => void session.refetch()}>
              Try again
            </Button>
          }
        >
          {session.error.message}
        </Alert>
      </div>
    );
  }

  if (!session.data) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  return (
    <SessionContext value={session.data}>
      <Outlet />
    </SessionContext>
  );
}
