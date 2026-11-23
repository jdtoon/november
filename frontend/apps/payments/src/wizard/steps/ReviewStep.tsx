import { useEffect, useRef } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Alert, Button, DescriptionList, FxQuotePanel, formatMoney } from "@november/ui";
import type { DescriptionItem } from "@november/ui";
import { ApiError, api, describeFieldErrors, emit, queryKeys, useFxQuote } from "@november/shared";
import type { CurrencyCode, PaymentDetail } from "@november/shared";
import { buildPayload } from "../payload";
import { parseAmount, quoteRequestFor } from "../routing";
import type { Routing } from "../routing";
import type { WizardData, WizardMode, WizardValues } from "../types";
import styles from "../Wizard.module.css";

type ReviewProps = {
  mode: WizardMode;
  values: WizardValues;
  data: WizardData;
  routing: Routing;
  onBack: () => void;
  onSubmitted: (payment: PaymentDetail) => void;
};

export function ReviewStep({ mode, values, data, routing, onBack, onSubmitted }: ReviewProps) {
  const client = useQueryClient();
  const fx = useFxQuote();
  const requested = useRef(false);
  const quoteRequest = quoteRequestFor(routing, values);

  useEffect(() => {
    if (requested.current || !quoteRequest) return;
    requested.current = true;
    void fx.request(quoteRequest);
  });

  const submit = useMutation({
    mutationFn: () => {
      const payload = buildPayload(mode, values, routing, fx.quote?.quoteId);
      return mode === "transfer"
        ? api.createInternationalTransfer(payload as Parameters<typeof api.createInternationalTransfer>[0])
        : api.createBeneficiaryPayment(payload as Parameters<typeof api.createBeneficiaryPayment>[0]);
    },
    onSuccess: (payment) => {
      void client.invalidateQueries({ queryKey: queryKeys.payments.all });
      void client.invalidateQueries({ queryKey: queryKeys.accounts });
      emit("november:payment-submitted", { paymentId: payment.id });
      onSubmitted(payment);
    },
    onError: (error) => {
      if (error instanceof ApiError && error.code.startsWith("fx_quote_")) fx.markExpired();
    },
  });

  const amount = parseAmount(values.amount) ?? 0;
  const amountCurrency = values.amountCurrency as CurrencyCode;
  const purpose = data.purposeCodes.find((candidate) => candidate.code === values.purposeCode);

  const items: DescriptionItem[] = [
    { term: "Pay from", description: routing.fromAccount?.name ?? "" },
    { term: mode === "transfer" ? "To account" : "Beneficiary", description: routing.destinationLabel },
  ];
  if (mode === "new") {
    const country = data.countries.find((candidate) => candidate.code === values.country);
    items.push({ term: "Bank", description: `${values.bankName.trim()}, ${country?.name ?? values.country}` });
    items.push({ term: "Save for future", description: values.saveForFuture ? "Yes" : "No" });
  }
  items.push({
    term: routing.requiresFx ? (routing.fixedSide === "Sell" ? "Amount leaving your account" : "Amount they receive") : "Amount",
    description: formatMoney(amount, amountCurrency),
  });
  if (purpose) items.push({ term: "Purpose", description: purpose.description });
  items.push({ term: "Reference", description: values.reference.trim() });

  const waitingForQuote = routing.requiresFx && (!fx.quote || fx.expired || fx.pending);
  const problem = submit.error instanceof ApiError ? submit.error : null;
  const problemLines = problem ? describeFieldErrors(problem) : [];

  return (
    <div className={styles.stack}>
      <DescriptionList items={items} />

      {routing.requiresFx ? (
        <FxQuotePanel
          quote={fx.quote}
          secondsLeft={fx.secondsLeft}
          validForSeconds={fx.quote?.validForSeconds}
          expired={fx.expired}
          loading={fx.pending}
          error={fx.error?.message}
          onRefresh={() => void fx.refresh()}
        />
      ) : (
        <Alert tone="info">Both sides are in the same currency, so no exchange rate applies.</Alert>
      )}

      {problem ? (
        <Alert tone="danger" title="Could not submit the payment">
          {problem.message}
          {problemLines.length ? (
            <ul>
              {problemLines.map((line) => (
                <li key={line}>{line}</li>
              ))}
            </ul>
          ) : null}
          {problem.code.startsWith("fx_quote_") ? " Get a new quote and submit again." : null}
        </Alert>
      ) : null}

      <div className={styles.buttons}>
        <Button variant="secondary" onClick={onBack} disabled={submit.isPending}>
          Back
        </Button>
        <Button onClick={() => submit.mutate()} loading={submit.isPending} disabled={waitingForQuote}>
          Submit for approval
        </Button>
      </div>
    </div>
  );
}
