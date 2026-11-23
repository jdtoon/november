import { useEffect, useRef, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, Button, Card, FxQuotePanel, TextArea } from "@november/ui";
import { ApiError, api, describeFieldErrors, emit, queryKeys, useFxQuote } from "@november/shared";
import type { PaymentDetail, Session } from "@november/shared";
import { approvalQuoteRequest, whyApprovalIsBlocked } from "./rules";
import styles from "./ApprovalActions.module.css";

type Mode = "idle" | "approve" | "reject";

const rejectSchema = z.object({
  reason: z.string().trim().min(5, "Give a reason of at least 5 characters").max(500, "Keep the reason under 500 characters"),
});

type RejectValues = z.infer<typeof rejectSchema>;

function problemLines(error: unknown): string[] {
  return error instanceof ApiError ? describeFieldErrors(error) : [];
}

export function ApprovalActions({ payment, session }: { payment: PaymentDetail; session: Session }) {
  const client = useQueryClient();
  const [mode, setMode] = useState<Mode>("idle");
  const [outcome, setOutcome] = useState<"approved" | "rejected" | null>(null);
  const blocked = whyApprovalIsBlocked(payment, session);

  const finish = (updated: PaymentDetail, result: "approved" | "rejected") => {
    client.setQueryData(queryKeys.payments.detail(payment.id), updated);
    void client.invalidateQueries({ queryKey: queryKeys.payments.all });
    emit("november:payment-decided", { paymentId: payment.id, outcome: result === "approved" ? "Processing" : "Rejected" });
    setOutcome(result);
    setMode("idle");
  };

  return (
    <Card title="Decision">
      <div className={styles.stack}>
        {outcome ? (
          <Alert tone="success" title={outcome === "approved" ? "Payment approved" : "Payment rejected"}>
            {outcome === "approved" ? "The bank is now processing this payment." : "The capturer will see your reason in the timeline."}
          </Alert>
        ) : null}

        {blocked && !outcome ? (
          <div id="approval-blocked">
            <Alert tone="warning" title="You cannot decide on this payment">
              {blocked}
            </Alert>
          </div>
        ) : null}

        {mode === "idle" ? (
          <div className={styles.buttons}>
            <Button disabled={Boolean(blocked)} aria-describedby={blocked ? "approval-blocked" : undefined} onClick={() => setMode("approve")}>
              Approve payment
            </Button>
            <Button variant="danger" disabled={Boolean(blocked)} aria-describedby={blocked ? "approval-blocked" : undefined} onClick={() => setMode("reject")}>
              Reject payment
            </Button>
          </div>
        ) : null}

        {mode === "approve" ? (
          <ApprovePanel payment={payment} onCancel={() => setMode("idle")} onApproved={(updated) => finish(updated, "approved")} />
        ) : null}
        {mode === "reject" ? (
          <RejectPanel payment={payment} onCancel={() => setMode("idle")} onRejected={(updated) => finish(updated, "rejected")} />
        ) : null}
      </div>
    </Card>
  );
}

function ApprovePanel({ payment, onCancel, onApproved }: { payment: PaymentDetail; onCancel: () => void; onApproved: (updated: PaymentDetail) => void }) {
  const fx = useFxQuote();
  const requested = useRef(false);

  useEffect(() => {
    if (!payment.requiresFx || requested.current) return;
    requested.current = true;
    void fx.request(approvalQuoteRequest(payment));
  });

  const approve = useMutation({
    mutationFn: () => api.approvePayment(payment.id, payment.requiresFx ? fx.quote?.quoteId : undefined),
    onSuccess: onApproved,
    onError: (error) => {
      if (error instanceof ApiError && error.code.startsWith("fx_quote_")) fx.markExpired();
    },
  });

  const waiting = payment.requiresFx && (!fx.quote || fx.expired || fx.pending);
  const lines = problemLines(approve.error);

  return (
    <div className={styles.stack}>
      {payment.requiresFx ? (
        <>
          <p>
            This payment involves a currency conversion. The original quote expired long ago, so you commit to the rate that applies now. Approve before the countdown ends.
          </p>
          <FxQuotePanel
            quote={fx.quote}
            secondsLeft={fx.secondsLeft}
            validForSeconds={fx.quote?.validForSeconds}
            expired={fx.expired}
            loading={fx.pending}
            error={fx.error?.message}
            onRefresh={() => void fx.refresh()}
          />
        </>
      ) : (
        <p>No currency conversion is involved. Approving sends the payment to the bank for processing.</p>
      )}

      {approve.isError ? (
        <Alert tone="danger" title="Could not approve the payment">
          {approve.error.message}
          {lines.length ? (
            <ul>
              {lines.map((line) => (
                <li key={line}>{line}</li>
              ))}
            </ul>
          ) : null}
        </Alert>
      ) : null}

      <div className={styles.buttons}>
        <Button onClick={() => approve.mutate()} loading={approve.isPending} disabled={waiting}>
          {payment.requiresFx ? "Approve at this rate" : "Confirm approval"}
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={approve.isPending}>
          Back
        </Button>
      </div>
    </div>
  );
}

function RejectPanel({ payment, onCancel, onRejected }: { payment: PaymentDetail; onCancel: () => void; onRejected: (updated: PaymentDetail) => void }) {
  const form = useForm<RejectValues>({ resolver: zodResolver(rejectSchema), defaultValues: { reason: "" } });
  const reject = useMutation({
    mutationFn: (values: RejectValues) => api.rejectPayment(payment.id, values.reason),
    onSuccess: onRejected,
  });

  return (
    <form className={styles.stack} noValidate onSubmit={form.handleSubmit((values) => reject.mutate(values))}>
      <TextArea
        label="Reason for rejecting"
        required
        hint="The capturer will see this."
        maxLength={500}
        error={form.formState.errors.reason?.message}
        {...form.register("reason")}
      />
      {reject.isError ? (
        <Alert tone="danger" title="Could not reject the payment">
          {reject.error.message}
        </Alert>
      ) : null}
      <div className={styles.buttons}>
        <Button type="submit" variant="danger" loading={reject.isPending}>
          Reject payment
        </Button>
        <Button variant="secondary" onClick={onCancel} disabled={reject.isPending}>
          Back
        </Button>
      </div>
    </form>
  );
}
