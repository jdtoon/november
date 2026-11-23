import { Route, Routes } from "react-router";
import { EmptyState } from "@november/ui";
import { RemoteProvider } from "@november/shared";
import type { RemoteAppProps } from "@november/shared";
import { ApprovalDetailPage } from "./pages/ApprovalDetailPage";
import { PendingListPage } from "./pages/PendingListPage";

export default function ApprovalsApp({ basePath, session }: RemoteAppProps) {
  return (
    <RemoteProvider basePath={basePath} session={session}>
      <Routes>
        <Route index element={<PendingListPage />} />
        <Route path=":paymentId" element={<ApprovalDetailPage />} />
        <Route path="*" element={<EmptyState title="Page not found" description="There is nothing at this address in Approvals." />} />
      </Routes>
    </RemoteProvider>
  );
}
