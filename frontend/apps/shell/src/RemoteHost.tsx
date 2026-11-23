import { Suspense, lazy, useMemo, useState } from "react";
import { Alert, Button, Spinner } from "@november/ui";
import { loadRemoteApp } from "./remotes";
import type { RemoteName } from "./remotes";
import { RemoteBoundary } from "./RemoteBoundary";
import { useSession } from "./session";

export type RemoteHostProps = {
  name: RemoteName;
  title: string;
};

export function RemoteHost({ name, title }: RemoteHostProps) {
  const session = useSession();
  const [attempt, setAttempt] = useState(0);
  const Remote = useMemo(() => lazy(() => loadRemoteApp(name, attempt > 0)), [name, attempt]);

  return (
    <RemoteBoundary
      key={`${name}-${attempt}`}
      fallback={(error) => (
        <Alert
          tone="warning"
          title={`${title} is temporarily unavailable`}
          actions={<Button variant="secondary" size="sm" onClick={() => setAttempt((count) => count + 1)}>Try again</Button>}
        >
          The rest of November still works. Try again in a moment.
          <details>
            <summary>Technical detail</summary>
            {error.message}
          </details>
        </Alert>
      )}
    >
      <Suspense fallback={<Spinner label={`Loading ${title}`} />}>
        <Remote basePath={`/${name}`} session={session} />
      </Suspense>
    </RemoteBoundary>
  );
}
