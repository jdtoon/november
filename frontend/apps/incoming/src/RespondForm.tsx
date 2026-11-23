import { useEffect, useMemo, useRef } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Alert, Button, Card, FxQuotePanel, Select, Spinner } from "@november/ui";
import { ApiError, api, describeFieldErrors, emit, queries, queryKeys, useFxQuote } from "@november/shared";
import type { IncomingPaymentDetail } from "@november/shared";
import styles from "./RespondForm.module.css";

type Values = { purposeCode: string };

export function RespondForm({ payment }: { payment: IncomingPaymentDetail }) {
  const client = useQueryClient();
  const needsPurpose = payment.actionsRequired.includes("PurposeCode");
  const needsFx = payment.actionsRequired.includes("FxAcceptance");

  const purposes = useQuery({ ...queries.purposeCodes("Incoming"), enabled: needsPurpose });
  const fx = useFxQuote();
  const requested = useRef(false);

  const schema = useMemo(
    () =>
      z.object({
        purposeCode: needsPurpose ? z.string().min(1, "Choose why you are receiving this money") : z.string(),
      }),
    [needsPurpose],
  );
  const form = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { purposeCode: "" } });

  useEffect(() => {
    if (!needsFx || requested.current) return;
    requested.current = true;
    void fx.request({
      sellCurrency: payment.amount.currency,
      buyCurrency: payment.creditAccount.currency,
      amount: payment.amount.amount,
      fixedSide: "Sell",
    });
  });

  const respond = useMutation({
    mutationFn: (values: Values) =>
      api.respondToIncomingPayment(payment.id, {
        purposeCode: needsPurpose ? values.purposeCode : undefined,
        fxQuoteId: needsFx ? fx.quote?.quoteId : undefined,
      }),
    onSuccess: (updated) => {
      client.setQueryData(queryKeys.incoming.detail(payment.id), updated);
      void client.invalidateQueries({ queryKey: queryKeys.incoming.all });
      emit("november:incoming-responded", { incomingId: payment.id });
    },
    onError: (error) => {
      if (error instanceof ApiError && error.code.startsWith("fx_quote_")) fx.markExpired();
    },
  });

  const waitingForQuote = needsFx && (!fx.quote || fx.expired || fx.pending);
  const problem = respond.error instanceof ApiError ? respond.error : null;
  const lines = problem ? describeFieldErrors(problem) : [];

  return (
    <Card title="What the bank needs from you">
      <form className={styles.form} noValidate onSubmit={form.handleSubmit((values) => respond.mutate(values))}>
        {needsPurpose ? (
          purposes.isPending ? (
            <Spinner label="Loading purposes" />
          ) : purposes.isError ? (
            <Alert tone="danger" title="Could not load purposes">
              {purposes.error.message}
            </Alert>
          ) : (
            <Select
              label="Purpose of this payment"
              required
              placeholder="Choose a purpose"
              hint="Banks need to know why money is being received, for regulatory reporting."
              error={form.formState.errors.purposeCode?.message}
              {...form.register("purposeCode")}
            >
              {purposes.data.map((purpose) => (
                <option key={purpose.code} value={purpose.code}>
                  {purpose.description}
                </option>
              ))}
            </Select>
          )
        ) : null}

        {needsFx ? (
          <div className={styles.fx}>
            <p>
              The money was sent in {payment.amount.currency} but your account holds {payment.creditAccount.currency}. Accept the rate below to have it converted.
            </p>
            <FxQuotePanel
              quote={fx.quote}
              secondsLeft={fx.secondsLeft}
              validForSeconds={fx.quote?.validForSeconds}
              expired={fx.expired}
              loading={fx.pending}
              error={fx.error?.message}
              onRefresh={() => void fx.refresh()}
              sellLabel="Sent to you"
              buyLabel="Credited to your account"
            />
          </div>
        ) : null}

        {problem ? (
          <Alert tone="danger" title="Could not submit">
            {problem.message}
            {lines.length ? (
              <ul>
                {lines.map((line) => (
                  <li key={line}>{line}</li>
                ))}
              </ul>
            ) : null}
          </Alert>
        ) : null}

        <div>
          <Button type="submit" loading={respond.isPending} disabled={waitingForQuote}>
            {needsFx ? "Accept rate and submit" : "Submit"}
          </Button>
        </div>
      </form>
    </Card>
  );
}
