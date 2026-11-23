import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router";
import { Alert, Button, Card, DataTable, MoneyAmount, PageHeader, Spinner, StatusBadge, buttonClassName, formatDateTime } from "@november/ui";
import type { Column } from "@november/ui";
import { queries } from "@november/shared";
import type { PaymentSummary } from "@november/shared";
import { useSession } from "./session";
import styles from "./DashboardPage.module.css";

const columns: Column<PaymentSummary>[] = [
  {
    key: "payee",
    header: "Payee",
    cell: (payment) => <Link to={`/payments/${payment.id}`}>{payment.payeeName}</Link>,
  },
  { key: "status", header: "Status", cell: (payment) => <StatusBadge status={payment.status} /> },
  { key: "reference", header: "Reference", cell: (payment) => payment.reference },
  { key: "captured", header: "Captured", cell: (payment) => formatDateTime(payment.capturedAt) },
  {
    key: "amount",
    header: "Amount",
    align: "right",
    cell: (payment) => <MoneyAmount amount={payment.sendAmount.amount} currency={payment.sendAmount.currency} />,
  },
];

export function DashboardPage() {
  const session = useSession();
  const dashboard = useQuery({ ...queries.dashboard(), refetchInterval: 30_000 });

  if (dashboard.isPending) return <Spinner label="Loading your dashboard" />;

  if (dashboard.isError) {
    return (
      <Alert
        tone="danger"
        title="Could not load the dashboard"
        actions={
          <Button variant="secondary" size="sm" onClick={() => void dashboard.refetch()}>
            Try again
          </Button>
        }
      >
        {dashboard.error.message}
      </Alert>
    );
  }

  const data = dashboard.data;
  const canCapture = session.roles.includes("Capturer");

  return (
    <>
      <PageHeader
        title={`Welcome, ${session.displayName.split(" ")[0]}`}
        subtitle={`${session.company.name}, here is where things stand.`}
        actions={
          canCapture ? (
            <Link to="/payments" className={buttonClassName("primary")}>
              Make a payment
            </Link>
          ) : undefined
        }
      />

      <section aria-labelledby="accounts-heading" className={styles.section}>
        <h2 id="accounts-heading" className={styles.heading}>
          Accounts
        </h2>
        <ul className={styles.accounts}>
          {data.accounts.map((account) => (
            <li key={account.id}>
              <Card>
                <p className={styles.accountName}>{account.name}</p>
                <p className={styles.balance}>
                  <MoneyAmount amount={account.balance} currency={account.currency} emphasis />
                </p>
                <p className={styles.available}>
                  Available <MoneyAmount amount={account.availableBalance} currency={account.currency} />
                </p>
                <p className={styles.number}>{account.iban}</p>
              </Card>
            </li>
          ))}
        </ul>
      </section>

      <section aria-labelledby="attention-heading" className={styles.section}>
        <h2 id="attention-heading" className={styles.heading}>
          Needs attention
        </h2>
        <ul className={styles.stats}>
          <li>
            <Link to="/approvals" className={styles.stat}>
              <span className={styles.statValue}>{data.awaitingMyApprovalCount}</span>
              <span>awaiting your approval</span>
            </Link>
          </li>
          <li>
            <Link to="/approvals" className={styles.stat}>
              <span className={styles.statValue}>{data.pendingApprovalCount}</span>
              <span>payments pending approval in total</span>
            </Link>
          </li>
          <li>
            <Link to="/incoming?status=ActionRequired" className={styles.stat}>
              <span className={styles.statValue}>{data.incomingActionRequiredCount}</span>
              <span>incoming payments need action</span>
            </Link>
          </li>
        </ul>
      </section>

      <Card
        title="Recent payments"
        padded={false}
        actions={
          <Link to="/payments" className={buttonClassName("ghost", "sm")}>
            View all
          </Link>
        }
      >
        <DataTable
          caption="Recent payments"
          columns={columns}
          rows={data.recentPayments}
          getRowKey={(payment) => payment.id}
          emptyTitle="No payments yet"
          emptyDescription="Payments you capture will show up here."
        />
      </Card>
    </>
  );
}
