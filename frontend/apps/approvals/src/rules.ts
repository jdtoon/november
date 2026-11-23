import { humanize } from "@november/ui";
import type { FxQuoteRequest, PaymentDetail, PaymentSummary, Session } from "@november/shared";

export function capturedByMe(payment: PaymentSummary, session: Session): boolean {
  if (payment.capturedBy === session.displayName || payment.capturedBy === session.userId) return true;
  return session.roles.includes("Approver") && payment.status === "PendingApproval" && !payment.canApprove;
}

export function whyApprovalIsBlocked(payment: PaymentSummary, session: Session): string | null {
  if (payment.canApprove) return null;
  if (payment.status !== "PendingApproval") {
    return `This payment is ${humanize(payment.status).toLowerCase()}, so it can no longer be approved or rejected.`;
  }
  if (!session.roles.includes("Approver")) return "You need the Approver role to approve or reject payments.";
  return "You captured this payment, so another approver is needed. The four-eyes rule stops you approving your own payments.";
}

export function approvalQuoteRequest(payment: PaymentDetail): FxQuoteRequest {
  const { fixedSide } = payment;
  return {
    sellCurrency: payment.fromAccount.currency,
    buyCurrency: payment.receiveAmount.currency,
    fixedSide,
    amount: fixedSide === "Sell" ? payment.sendAmount.amount : payment.receiveAmount.amount,
  };
}
