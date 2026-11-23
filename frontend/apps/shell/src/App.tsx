import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { Link, Route, Routes } from "react-router";
import { EmptyState, buttonClassName } from "@november/ui";
import { on, queryKeys } from "@november/shared";
import { DashboardPage } from "./DashboardPage";
import { Layout } from "./Layout";
import { LoginPage } from "./LoginPage";
import { RemoteHost } from "./RemoteHost";
import { RequireSession } from "./session";

function NotFound() {
  return (
    <EmptyState
      title="Page not found"
      description="That address does not exist in November."
      action={
        <Link to="/" className={buttonClassName("primary")}>
          Back to the dashboard
        </Link>
      }
    />
  );
}

function useShellEvents() {
  const client = useQueryClient();

  useEffect(() => {
    const refreshDashboard = () => void client.invalidateQueries({ queryKey: queryKeys.dashboard });
    const unsubscribe = [
      on("november:payment-submitted", refreshDashboard),
      on("november:payment-decided", refreshDashboard),
      on("november:incoming-responded", refreshDashboard),
      on("november:session-expired", () => {
        client.removeQueries({ predicate: (query) => query.queryKey[0] !== "session" });
        client.setQueryData(queryKeys.session, null);
      }),
    ];
    return () => unsubscribe.forEach((off) => off());
  }, [client]);
}

export function App() {
  useShellEvents();

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RequireSession />}>
        <Route element={<Layout />}>
          <Route index element={<DashboardPage />} />
          <Route path="payments/*" element={<RemoteHost name="payments" title="Payments" />} />
          <Route path="approvals/*" element={<RemoteHost name="approvals" title="Approvals" />} />
          <Route path="incoming/*" element={<RemoteHost name="incoming" title="Incoming payments" />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Route>
    </Routes>
  );
}
