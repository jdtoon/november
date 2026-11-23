import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { QueryClientProvider, useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BrowserRouter, Route, Routes } from "react-router";
import { Alert, Button, Card, Chip, PageHeader, Spinner } from "@november/ui";
import { api } from "../api/endpoints";
import { queries } from "../api/queries";
import { queryKeys } from "../api/queryKeys";
import { createQueryClient } from "../queryClient";
import { on } from "../events";
import type { RemoteAppProps } from "../remote";
import styles from "./StandaloneHost.module.css";

export type StandaloneHostProps = {
  name: string;
  children: (props: RemoteAppProps) => ReactNode;
};

function Gate({ name, children }: StandaloneHostProps) {
  const client = useQueryClient();
  const session = useQuery(queries.session());
  const personas = useQuery({ ...queries.personas(), enabled: session.data === null });
  const login = useMutation({
    mutationFn: api.login,
    onSuccess: (next) => client.setQueryData(queryKeys.session, next),
  });
  const logout = useMutation({
    mutationFn: api.logout,
    onSuccess: () => client.setQueryData(queryKeys.session, null),
  });

  useEffect(() => on("november:session-expired", () => client.setQueryData(queryKeys.session, null)), [client]);

  if (session.isPending) {
    return (
      <main className={styles.page}>
        <Spinner label="Checking your session" />
      </main>
    );
  }

  if (session.isError) {
    return (
      <main className={styles.page}>
        <Alert tone="danger" title="Could not reach the BFF">
          Start the BFF on http://localhost:5080 and reload this page.
        </Alert>
      </main>
    );
  }

  if (session.data === null) {
    return (
      <main className={styles.page}>
        <PageHeader title={`${name} (standalone)`} subtitle="Pick a persona to sign in. Only used when this app runs without the shell." />
        {login.isError ? <Alert tone="danger">{login.error.message}</Alert> : null}
        <div className={styles.personas}>
          {personas.data?.map((persona) => (
            <Card key={persona.personaId} title={persona.displayName}>
              <p>{persona.description}</p>
              <p className={styles.roles}>{persona.roles.join(", ")}</p>
              <Button onClick={() => login.mutate(persona.personaId)} loading={login.isPending}>
                Sign in as {persona.displayName}
              </Button>
            </Card>
          ))}
        </div>
      </main>
    );
  }

  const current = session.data;
  return (
    <>
      <header className={styles.bar}>
        <strong>{name} (standalone)</strong>
        <span className={styles.user}>
          {current.displayName}
          {current.roles.map((role) => (
            <Chip key={role}>{role}</Chip>
          ))}
        </span>
        <Button variant="secondary" size="sm" onClick={() => logout.mutate()}>
          Sign out
        </Button>
      </header>
      <main className={styles.page}>
        <Routes>
          <Route path="/*" element={children({ basePath: "", session: current })} />
        </Routes>
      </main>
    </>
  );
}

export function StandaloneHost(props: StandaloneHostProps) {
  const [client] = useState(createQueryClient);
  return (
    <QueryClientProvider client={client}>
      <BrowserRouter>
        <Gate {...props} />
      </BrowserRouter>
    </QueryClientProvider>
  );
}
