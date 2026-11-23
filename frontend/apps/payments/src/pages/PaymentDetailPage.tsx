import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useParams } from "react-router";
import { Alert, Button, Card, EmptyState, PageHeader, Spinner, Timeline } from "@november/ui";
import { ApiError, PaymentFacts, api, emit, queries, queryKeys, useRemote } from "@november/shared";
import styles from "./PaymentDetailPage.module.css";

export function PaymentDetailPage() {
  const { paymentId = "" } = useParams();
  const { href } = useRemote();
  const client = useQueryClient();
  const [confirming, setConfirming] = useState(false);

  const payment = useQuery({
    ...queries.payment(paymentId),
    refetchInterval: (query) => (query.state.data?.status === "Processing" ? 2000 : false),
  });

  const cancel = useMutation({
    mutationFn: () => api.cancelPayment(paymentId),
    onSuccess: (updated) => {
      client.setQueryData(queryKeys.payments.detail(paymentId), updated);
      void client.invalidateQueries({ queryKey: queryKeys.payments.all });
      emit("november:payment-decided", { paymentId, outcome: "Cancelled" });
      setConfirming(false);
    },
  });

  const back = <Link to={href()}>Back to payments</Link>;

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
      <PageHeader title={data.payeeName} subtitle={`Reference ${data.reference}`} back={back} />

      <div className={styles.stack}>
        {data.status === "PendingApproval" ? (
          <Alert tone="warning" title="Pending approval">
            {data.canCancel
              ? "A second person with the Approver role must approve this payment. You can cancel it until then."
              : "A second person with the Approver role must approve this payment before the bank processes it."}
          </Alert>
        ) : null}

        {cancel.isSuccess ? <Alert tone="success" title="Payment cancelled" /> : null}
        {cancel.isError ? (
          <Alert tone="danger" title="Could not cancel the payment">
            {cancel.error.message}
          </Alert>
        ) : null}

        <Card title="Payment details">
          <PaymentFacts payment={data} />
        </Card>

        {data.canCancel ? (
          <Card title="Cancel this payment">
            {confirming ? (
              <div className={styles.confirm}>
                <p>Cancelling stops the payment for good. You would need to capture it again.</p>
                <div className={styles.buttons}>
                  <Button variant="danger" loading={cancel.isPending} onClick={() => cancel.mutate()}>
                    Yes, cancel payment
                  </Button>
                  <Button variant="secondary" onClick={() => setConfirming(false)} disabled={cancel.isPending}>
                    Keep payment
                  </Button>
                </div>
              </div>
            ) : (
              <Button variant="secondary" onClick={() => setConfirming(true)}>
                Cancel payment
              </Button>
            )}
          </Card>
        ) : null}

        <Card title="Timeline">
          <Timeline events={data.timeline} />
        </Card>
      </div>
    </>
  );
}
