import { Route, Routes } from "react-router";
import { EmptyState } from "@november/ui";
import { RemoteProvider } from "@november/shared";
import type { RemoteAppProps } from "@november/shared";
import { PaymentDetailPage } from "./pages/PaymentDetailPage";
import { PaymentsHubPage } from "./pages/PaymentsHubPage";
import { PaymentWizard } from "./wizard/PaymentWizard";

export default function PaymentsApp({ basePath, session }: RemoteAppProps) {
  return (
    <RemoteProvider basePath={basePath} session={session}>
      <Routes>
        <Route index element={<PaymentsHubPage />} />
        <Route path="beneficiary" element={<PaymentWizard mode="saved" />} />
        <Route path="new-beneficiary" element={<PaymentWizard mode="new" />} />
        <Route path="transfer" element={<PaymentWizard mode="transfer" />} />
        <Route path=":paymentId" element={<PaymentDetailPage />} />
        <Route path="*" element={<EmptyState title="Page not found" description="There is nothing at this address in Payments." />} />
      </Routes>
    </RemoteProvider>
  );
}
