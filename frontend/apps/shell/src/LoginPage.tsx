import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Navigate, useLocation, useNavigate } from "react-router";
import { Alert, Button, Card, Chip, PageHeader, Spinner } from "@november/ui";
import { api, queries, queryKeys } from "@november/shared";
import styles from "./LoginPage.module.css";

export function LoginPage() {
  const client = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const target = (location.state as { from?: string } | null)?.from ?? "/";

  const session = useQuery(queries.session());
  const personas = useQuery(queries.personas());
  const login = useMutation({
    mutationFn: api.login,
    onSuccess: (next) => {
      client.setQueryData(queryKeys.session, next);
      navigate(target, { replace: true });
    },
  });

  if (session.data) return <Navigate to={target} replace />;

  return (
    <div className={styles.page}>
      <div className={styles.panel}>
        <PageHeader
          title="Sign in to November"
          subtitle="Harbourline Trading Ltd, business banking. This is a demo: choose a persona instead of entering a password."
        />
        {login.isError ? (
          <Alert tone="danger" title="Could not sign in">
            {login.error.message}
          </Alert>
        ) : null}
        {personas.isPending ? <Spinner label="Loading personas" /> : null}
        {personas.isError ? (
          <Alert
            tone="danger"
            title="Could not load personas"
            actions={
              <Button variant="secondary" size="sm" onClick={() => void personas.refetch()}>
                Try again
              </Button>
            }
          >
            {personas.error.message}
          </Alert>
        ) : null}
        <ul className={styles.list}>
          {personas.data?.map((persona) => (
            <li key={persona.personaId}>
              <Card title={persona.displayName}>
                <p className={styles.description}>{persona.description}</p>
                <p className={styles.roles}>
                  {persona.roles.map((role) => (
                    <span key={role} className={styles.role}>
                      <Chip>{role}</Chip>
                    </span>
                  ))}
                </p>
                <Button
                  onClick={() => login.mutate(persona.personaId)}
                  loading={login.isPending && login.variables === persona.personaId}
                  disabled={login.isPending}
                >
                  Sign in as {persona.displayName}
                </Button>
              </Card>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
