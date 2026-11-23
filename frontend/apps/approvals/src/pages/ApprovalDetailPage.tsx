import { useQuery } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { Alert, Button, Card, EmptyState, PageHeader, Spinner, Timeline } from "@november/ui";
import { ApiError, PaymentFacts, queries, useRemote } from "@november/shared";
import { ApprovalActions } from "../ApprovalActions";
import styles from "./ApprovalDetailPage.module.css";

export function ApprovalDetailPage() {
  const { paymentId = "" } = useParams();
  const { href, session } = useRemote();
  const payment = useQuery({
    ...queries.payment(paymentId),
    refetchInterval: (query) => (query.state.data?.status === "Processing" ? 2000 : false),
  });

  const back = <Link to={href()}>Back to approvals</Link>;

  if (payment.isPending) return <Spinner label="Loading payment" />;

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

  return (
    <>
      <PageHeader title={`Review: ${data.payeeName}`} subtitle={`Reference ${data.reference}`} back={back} />
      <div className={styles.stack}>
        <Card title="Payment details">
          <PaymentFacts payment={data} />
        </Card>
        <ApprovalActions payment={data} session={session} />
        <Card title="Timeline">
          <Timeline events={data.timeline} />
        </Card>
      </div>
    </>
  );
}
