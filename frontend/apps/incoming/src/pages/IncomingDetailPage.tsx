import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { Alert, Button, Card, DescriptionList, EmptyState, MoneyAmount, PageHeader, Spinner, StatusBadge, Timeline, formatDateTime, formatRate } from "@november/ui";
import type { DescriptionItem } from "@november/ui";
import { ApiError, queries, useRemote } from "@november/shared";
import { RespondForm } from "../RespondForm";
import styles from "./IncomingDetailPage.module.css";

export function IncomingDetailPage() {
  const { incomingId = "" } = useParams();
  const { href } = useRemote();
  const payment = useQuery({
    ...queries.incomingPayment(incomingId),
    refetchInterval: (query) => (query.state.data?.status === "Processing" ? 2000 : false),
  });

  const back = <Link to={href()}>Back to incoming payments</Link>;

  if (payment.isPending) return <Spinner label="Loading incoming payment" />;

  if (payment.isError) {
    const notFound = payment.error instanceof ApiError && payment.error.status === 404;
    return notFound ? (
      <EmptyState title="Payment not found" description="It may have been removed, or the link is wrong." action={back} />
    ) : (
      <Alert
        tone="danger"
        title="Could not load this payment"
        actions={
          <Button variant="secondary" size="sm" onClick={() => void payment.refetch()}>
            Try again
          </Button>
        }
      >
        {payment.error.message}
      </Alert>
    );
  }

  const data = payment.data;
  const items: DescriptionItem[] = [
    { term: "Status", description: <StatusBadge status={data.status} /> },
    { term: "Sender", description: `${data.senderName} (${data.senderCountry})` },
    { term: "Sender's bank", description: `${data.senderBank.name}, ${data.senderBank.swiftBic}` },
    { term: "Sender's reference", description: data.senderReference },
    { term: "Amount sent to you", description: <MoneyAmount amount={data.amount.amount} currency={data.amount.currency} /> },
    { term: "Receiving account", description: data.creditAccount.name },
    { term: "Received", description: formatDateTime(data.receivedAt) },
  ];
  if (data.purpose) items.push({ term: "Purpose", description: data.purpose.description });
  if (data.fx) items.push({ term: "Exchange rate", description: `${formatRate(data.fx.rate)} (accepted by ${data.fx.acceptedBy})` });
  if (data.creditedAmount) {
    items.push({ term: "Credited", description: <MoneyAmount amount={data.creditedAmount.amount} currency={data.creditedAmount.currency} /> });
  }

  return (
    <>
      <PageHeader title={`From ${data.senderName}`} subtitle={`Reference ${data.senderReference}`} back={back} />
      <div className={styles.stack}>
        {data.status === "Processing" ? (
          <Alert tone="info" title="Processing">
            <Spinner size="sm" label="Waiting for the funds to reach your account. This page updates by itself." />
          </Alert>
        ) : null}
        {data.status === "Credited" ? (
          <Alert tone="success" title="Credited">
            The funds are in {data.creditAccount.name}.
          </Alert>
        ) : null}
        {data.status === "Cancelled" ? (
          <Alert tone="warning" title="Cancelled">
            The bank cancelled this payment. Nothing was credited.
          </Alert>
        ) : null}

        <Card title="Payment details">
          <DescriptionList items={items} />
        </Card>

        {data.status === "ActionRequired" && data.actionsRequired.length ? <RespondForm payment={data} /> : null}

        <Card title="Timeline">
          <Timeline events={data.timeline} />
        </Card>
      </div>
    </>
  );
}
