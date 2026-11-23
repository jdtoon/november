import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { Alert, Button, Card, DataTable, MoneyAmount, PageHeader, Spinner, buttonClassName, formatDateTime } from "@november/ui";
import type { Column } from "@november/ui";
import { queries, useRemote } from "@november/shared";
import type { PaymentSummary } from "@november/shared";
import { capturedByMe } from "../rules";
import styles from "./PendingListPage.module.css";

export function PendingListPage() {
  const { href, session } = useRemote();
  const pending = useQuery({ ...queries.payments({ status: "PendingApproval" }), refetchInterval: 15_000 });

  const columns: Column<PaymentSummary>[] = [
    {
      key: "payee",
      header: "Payee",
      cell: (payment) => (
        <div>
          <Link to={href(payment.id)}>{payment.payeeName}</Link>
          <div className={styles.reference}>{payment.reference}</div>
        </div>
      ),
    },
    { key: "type", header: "Type", cell: (payment) => (payment.type === "InternationalTransfer" ? "Transfer" : "Payment") },
    { key: "captured", header: "Captured by", cell: (payment) => `${payment.capturedBy}, ${formatDateTime(payment.capturedAt)}` },
    {
      key: "amount",
      header: "Amount",
      align: "right",
      cell: (payment) => <MoneyAmount amount={payment.sendAmount.amount} currency={payment.sendAmount.currency} />,
    },
    {
      key: "action",
      header: "Action",
      cell: (payment) =>
        capturedByMe(payment, session) ? (
          <span className={styles.flag}>You captured this, so another approver is needed</span>
        ) : (
          <Link to={href(payment.id)} className={buttonClassName("primary", "sm")}>
            Review
          </Link>
        ),
    },
  ];

  return (
    <>
      <PageHeader title="Approvals" subtitle="Payments and transfers waiting for a second pair of eyes." />

      {!session.roles.includes("Approver") ? (
        <div className={styles.notice}>
          <Alert tone="info" title="View only">
            Your role does not include approving payments. Sign in as Sam Okafor or Jordan Reeves to approve or reject.
          </Alert>
        </div>
      ) : null}

      <Card title="Pending approval" padded={false}>
        {pending.isPending ? (
          <div className={styles.pad}>
            <Spinner label="Loading pending payments" />
          </div>
        ) : null}
        {pending.isError ? (
          <div className={styles.pad}>
            <Alert
              tone="danger"
              title="Could not load pending payments"
              actions={
                <Button variant="secondary" size="sm" onClick={() => void pending.refetch()}>
                  Try again
                </Button>
              }
            >
              {pending.error.message}
            </Alert>
          </div>
        ) : null}
        {pending.data ? (
          <DataTable
            caption="Payments pending approval"
            columns={columns}
            rows={pending.data}
            getRowKey={(payment) => payment.id}
            emptyTitle="Nothing is waiting for approval"
            emptyDescription="New payments appear here as soon as a capturer submits them."
          />
        ) : null}
      </Card>
    </>
  );
}
