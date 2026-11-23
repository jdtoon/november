import { Route, Routes } from "react-router";
import { EmptyState } from "@november/ui";
import { RemoteProvider } from "@november/shared";
import type { RemoteAppProps } from "@november/shared";
import { IncomingDetailPage } from "./pages/IncomingDetailPage";
import { IncomingListPage } from "./pages/IncomingListPage";

export default function IncomingApp({ basePath, session }: RemoteAppProps) {
  return (
    <RemoteProvider basePath={basePath} session={session}>
      <Routes>
        <Route index element={<IncomingListPage />} />
        <Route path=":incomingId" element={<IncomingDetailPage />} />
        <Route path="*" element={<EmptyState title="Page not found" description="There is nothing at this address in Incoming payments." />} />
      </Routes>
    </RemoteProvider>
  );
}
