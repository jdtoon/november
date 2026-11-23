import { Link } from "react-router";
import { Alert, Card, PageHeader, StatusBadge, buttonClassName } from "@november/ui";
import { PaymentFacts, useRemote } from "@november/shared";
import type { PaymentDetail } from "@november/shared";
import styles from "../Wizard.module.css";

export function Confirmation({ payment }: { payment: PaymentDetail }) {
  const { href } = useRemote();

  return (
    <>
      <PageHeader title="Payment submitted" subtitle={`Reference ${payment.reference}`} />
      <div className={styles.stack}>
        <Alert tone="success" title={<>Status: <StatusBadge status={payment.status} label="Pending approval" /></>}>
          Nothing has left your account yet. A second person with the Approver role must approve this payment first.
        </Alert>
        <Card title="What you submitted">
          <PaymentFacts payment={payment} />
        </Card>
        <div className={styles.buttons}>
          <Link to={href(payment.id)} className={buttonClassName("primary")}>
            View payment
          </Link>
          <Link to={href()} className={buttonClassName("secondary")}>
            Back to payments
          </Link>
        </div>
      </div>
    </>
  );
}
