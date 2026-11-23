import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router";
import { Alert, Button, Card, DataTable, FilterTabs, MoneyAmount, PageHeader, Spinner, StatusBadge, formatDateTime, humanize } from "@november/ui";
import type { Column } from "@november/ui";
import { queries, useRemote } from "@november/shared";
import type { PaymentStatus, PaymentSummary } from "@november/shared";
import styles from "./PaymentsHubPage.module.css";

const statuses: PaymentStatus[] = ["PendingApproval", "Processing", "Completed", "Rejected", "Cancelled"];
const filterOptions = [{ value: "all", label: "All" }, ...statuses.map((status) => ({ value: status, label: humanize(status) }))];

export function PaymentsHubPage() {
  const { href, hasRole } = useRemote();
  const [params, setParams] = useSearchParams();
  const selected = params.get("status") ?? "all";
  const status = statuses.find((candidate) => candidate === selected);

  const payments = useQuery({
    ...queries.payments(status ? { status } : {}),
    refetchInterval: (query) => (query.state.data?.some((payment) => payment.status === "Processing") ? 3000 : false),
  });

  const columns: Column<PaymentSummary>[] = [
    { key: "payee", header: "Payee", cell: (payment) => <Link to={href(payment.id)}>{payment.payeeName}</Link> },
    { key: "type", header: "Type", cell: (payment) => (payment.type === "InternationalTransfer" ? "Transfer" : "Payment") },
    { key: "status", header: "Status", cell: (payment) => <StatusBadge status={payment.status} /> },
    { key: "reference", header: "Reference", cell: (payment) => payment.reference },
    { key: "captured", header: "Captured", cell: (payment) => formatDateTime(payment.capturedAt) },
    {
      key: "amount",
      header: "Amount sent",
      align: "right",
      cell: (payment) => <MoneyAmount amount={payment.sendAmount.amount} currency={payment.sendAmount.currency} />,
    },
  ];

  return (
    <>
      <PageHeader title="Payments" subtitle="Capture a payment, then a second person approves it before the bank processes it." />

      {hasRole("Capturer") ? (
        <ul className={styles.entries}>
          <li>
            <Link to={href("beneficiary")} className={styles.entry}>
              <span className={styles.entryTitle}>Pay a saved beneficiary</span>
              <span className={styles.entryText}>Choose someone you have paid before and send money abroad.</span>
            </Link>
          </li>
          <li>
            <Link to={href("new-beneficiary")} className={styles.entry}>
              <span className={styles.entryTitle}>Pay a new beneficiary</span>
              <span className={styles.entryText}>Enter their bank details once, and save them for next time if you like.</span>
            </Link>
          </li>
          <li>
            <Link to={href("transfer")} className={styles.entry}>
              <span className={styles.entryTitle}>International transfer</span>
              <span className={styles.entryText}>Move money between your own accounts, converting currency if needed.</span>
            </Link>
          </li>
        </ul>
      ) : (
        <Alert tone="info" title="View only">
          Your role does not include capturing payments. You can still follow the payments below.
        </Alert>
      )}

      <Card
        title="Your payments"
        padded={false}
        actions={<FilterTabs label="Filter by status" options={filterOptions} value={status ?? "all"} onChange={(value) => setParams(value === "all" ? {} : { status: value })} />}
      >
        {payments.isPending ? (
          <div className={styles.pad}>
            <Spinner label="Loading payments" />
          </div>
        ) : null}
        {payments.isError ? (
          <div className={styles.pad}>
            <Alert
              tone="danger"
              title="Could not load payments"
              actions={
                <Button variant="secondary" size="sm" onClick={() => void payments.refetch()}>
                  Try again
                </Button>
              }
            >
              {payments.error.message}
            </Alert>
          </div>
        ) : null}
        {payments.data ? (
          <DataTable
            caption="Payments"
            columns={columns}
            rows={payments.data}
            getRowKey={(payment) => payment.id}
            emptyTitle="No payments found"
            emptyDescription={status ? "Try a different status filter." : "Start with one of the options above."}
          />
        ) : null}
      </Card>
    </>
  );
}
