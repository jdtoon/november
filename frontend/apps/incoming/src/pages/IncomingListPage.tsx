import { useQuery } from "@tanstack/react-query";
import { Link, useSearchParams } from "react-router";
import { Alert, Button, Card, DataTable, FilterTabs, MoneyAmount, PageHeader, Spinner, StatusBadge, formatDateTime, humanize } from "@november/ui";
import type { Column } from "@november/ui";
import { queries, useRemote } from "@november/shared";
import type { IncomingPaymentSummary, IncomingStatus } from "@november/shared";
import styles from "./IncomingListPage.module.css";

const statuses: IncomingStatus[] = ["ActionRequired", "Processing", "Credited", "Cancelled"];
const filterOptions = [{ value: "all", label: "All" }, ...statuses.map((status) => ({ value: status, label: humanize(status) }))];

const actionLabels = { PurposeCode: "Add a purpose", FxAcceptance: "Accept the exchange rate" } as const;

export function IncomingListPage() {
  const { href } = useRemote();
  const [params, setParams] = useSearchParams();
  const status = statuses.find((candidate) => candidate === params.get("status"));

  const incoming = useQuery({
    ...queries.incomingPayments(status ? { status } : {}),
    refetchInterval: (query) => (query.state.data?.some((payment) => payment.status === "Processing") ? 3000 : false),
  });

  const columns: Column<IncomingPaymentSummary>[] = [
    {
      key: "sender",
      header: "Sender",
      cell: (payment) => (
        <div>
          <Link to={href(payment.id)}>{payment.senderName}</Link>
          <div className={styles.muted}>{payment.senderCountry}</div>
        </div>
      ),
    },
    { key: "status", header: "Status", cell: (payment) => <StatusBadge status={payment.status} /> },
    {
      key: "needs",
      header: "What we need",
      cell: (payment) =>
        payment.status === "ActionRequired" && payment.actionsRequired.length
          ? payment.actionsRequired.map((action) => actionLabels[action]).join(", ")
          : "Nothing",
    },
    { key: "account", header: "Credit account", cell: (payment) => payment.creditAccountName },
    { key: "received", header: "Received", cell: (payment) => formatDateTime(payment.receivedAt) },
    {
      key: "amount",
      header: "Amount",
      align: "right",
      cell: (payment) => <MoneyAmount amount={payment.amount.amount} currency={payment.amount.currency} />,
    },
  ];

  return (
    <>
      <PageHeader title="Incoming payments" subtitle="Money on its way to your accounts. Some payments need a little information before they are credited." />
      <Card
        title="Payments"
        padded={false}
        actions={<FilterTabs label="Filter by status" options={filterOptions} value={status ?? "all"} onChange={(value) => setParams(value === "all" ? {} : { status: value })} />}
      >
        {incoming.isPending ? (
          <div className={styles.pad}>
            <Spinner label="Loading incoming payments" />
          </div>
        ) : null}
        {incoming.isError ? (
          <div className={styles.pad}>
            <Alert
              tone="danger"
              title="Could not load incoming payments"
              actions={
                <Button variant="secondary" size="sm" onClick={() => void incoming.refetch()}>
                  Try again
                </Button>
              }
            >
              {incoming.error.message}
            </Alert>
          </div>
        ) : null}
        {incoming.data ? (
          <DataTable
            caption="Incoming payments"
            columns={columns}
            rows={incoming.data}
            getRowKey={(payment) => payment.id}
            emptyTitle="No incoming payments here"
            emptyDescription={status ? "Try a different status." : "Payments sent to you will appear here."}
          />
        ) : null}
      </Card>
    </>
  );
}
