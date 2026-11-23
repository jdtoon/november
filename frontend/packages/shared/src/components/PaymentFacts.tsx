import { Alert, DescriptionList, MoneyAmount, StatusBadge, formatDateTime, formatRate, humanize } from "@november/ui";
import type { DescriptionItem } from "@november/ui";
import type { PaymentDetail } from "../types";

export function PaymentFacts({ payment }: { payment: PaymentDetail }) {
  const items: DescriptionItem[] = [
    { term: "Status", description: <StatusBadge status={payment.status} /> },
    { term: "Type", description: humanize(payment.type) },
    { term: "From account", description: payment.fromAccount.name },
    { term: "You send", description: <MoneyAmount amount={payment.sendAmount.amount} currency={payment.sendAmount.currency} /> },
    { term: "They receive", description: <MoneyAmount amount={payment.receiveAmount.amount} currency={payment.receiveAmount.currency} /> },
  ];

  if (payment.beneficiary) {
    const b = payment.beneficiary;
    items.push({ term: "Beneficiary", description: b.name });
    items.push({ term: "Bank", description: `${b.bankName} (${b.country})` });
    if (b.iban) items.push({ term: "IBAN", description: b.iban });
    if (b.accountNumber) items.push({ term: "Account number", description: b.accountNumber });
    if (b.routingNumber) items.push({ term: "Routing number", description: b.routingNumber });
    items.push({ term: "SWIFT / BIC", description: b.swiftBic });
  } else if (payment.toAccount) {
    items.push({ term: "To account", description: payment.toAccount.name });
  }

  if (payment.purpose) items.push({ term: "Purpose", description: payment.purpose.description });
  items.push({ term: "Reference", description: payment.reference });
  if (payment.fx) {
    items.push({ term: "Exchange rate", description: `${formatRate(payment.fx.rate)} (accepted by ${payment.fx.acceptedBy})` });
  }
  items.push({ term: "Captured by", description: `${payment.capturedBy}, ${formatDateTime(payment.capturedAt)}` });

  return (
    <>
      {payment.rejectionReason ? (
        <Alert tone="danger" title="Rejected">
          {payment.rejectionReason}
        </Alert>
      ) : null}
      <DescriptionList items={items} />
    </>
  );
}
