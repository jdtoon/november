import { useEffect, useMemo, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, useWatch } from "react-hook-form";
import { Link } from "react-router";
import { Alert, Button, Card, PageHeader, Spinner, Stepper } from "@november/ui";
import { queries, useRemote } from "@november/shared";
import type { PaymentDetail } from "@november/shared";
import { AmountStep } from "./steps/AmountStep";
import { Confirmation } from "./steps/Confirmation";
import { DetailsStep } from "./steps/DetailsStep";
import { RecipientStep } from "./steps/RecipientStep";
import { ReviewStep } from "./steps/ReviewStep";
import { describeRouting } from "./routing";
import { emptyValues, modeTitles, stepFields, stepLabels } from "./types";
import type { WizardData, WizardMode, WizardValues } from "./types";
import { buildSchema } from "./validation";
import styles from "./Wizard.module.css";

export function PaymentWizard({ mode }: { mode: WizardMode }) {
  const { hasRole, href } = useRemote();
  const accounts = useQuery(queries.accounts());
  const beneficiaries = useQuery(queries.beneficiaries());
  const countries = useQuery(queries.countries());
  const currencies = useQuery(queries.currencies());
  const purposeCodes = useQuery(queries.purposeCodes("Outgoing"));

  const all = [accounts, beneficiaries, countries, currencies, purposeCodes];
  const failed = all.find((query) => query.isError);
  const back = <Link to={href()}>Back to payments</Link>;

  if (!hasRole("Capturer")) {
    return (
      <>
        <PageHeader title={modeTitles[mode]} back={back} />
        <Alert tone="info" title="You need the Capturer role">
          Only capturers can start payments. Sign in as Alex Morgan or Jordan Reeves to try this journey.
        </Alert>
      </>
    );
  }

  if (failed?.error) {
    return (
      <>
        <PageHeader title={modeTitles[mode]} back={back} />
        <Alert
          tone="danger"
          title="Could not load what this form needs"
          actions={
            <Button variant="secondary" size="sm" onClick={() => all.forEach((query) => void query.refetch())}>
              Try again
            </Button>
          }
        >
          {failed.error.message}
        </Alert>
      </>
    );
  }

  if (!accounts.data || !beneficiaries.data || !countries.data || !currencies.data || !purposeCodes.data) {
    return <Spinner label="Preparing the payment form" />;
  }

  const data: WizardData = {
    accounts: accounts.data,
    beneficiaries: beneficiaries.data,
    countries: countries.data,
    currencies: currencies.data,
    purposeCodes: purposeCodes.data,
  };

  return <WizardForm mode={mode} data={data} />;
}

function WizardForm({ mode, data }: { mode: WizardMode; data: WizardData }) {
  const { href } = useRemote();
  const schema = useMemo(() => buildSchema(mode, data.countries), [mode, data.countries]);
  const form = useForm<WizardValues>({
    resolver: zodResolver(schema),
    mode: "onTouched",
    defaultValues: { ...emptyValues, fromAccountId: mode === "transfer" ? "" : (data.accounts[0]?.id ?? "") },
  });

  const [step, setStep] = useState(0);
  const [submitted, setSubmitted] = useState<PaymentDetail | null>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const labels = stepLabels[mode];
  const lastStep = labels.length - 1;

  const watched = useWatch({ control: form.control });
  const routing = describeRouting(mode, { ...emptyValues, ...watched }, data);

  useEffect(() => {
    headingRef.current?.focus();
  }, [step]);

  const next = async () => {
    const valid = await form.trigger(stepFields[mode][step]);
    if (valid) setStep((current) => current + 1);
  };

  if (submitted) {
    return <Confirmation payment={submitted} />;
  }

  return (
    <>
      <PageHeader title={modeTitles[mode]} back={<Link to={href()}>Back to payments</Link>} />
      <div className={styles.stepper}>
        <Stepper steps={labels} current={step} label={`${modeTitles[mode]} progress`} />
      </div>
      <Card>
        <form
          noValidate
          className={styles.form}
          onSubmit={(event) => {
            event.preventDefault();
            if (step < lastStep) void next();
          }}
        >
          <h2 ref={headingRef} tabIndex={-1} className={styles.stepTitle}>
            Step {step + 1} of {labels.length}: {labels[step]}
          </h2>

          {step === 0 ? <RecipientStep mode={mode} form={form} data={data} routing={routing} /> : null}
          {step === 1 ? <AmountStep mode={mode} form={form} data={data} routing={routing} /> : null}
          {step === 2 ? <DetailsStep mode={mode} form={form} data={data} /> : null}
          {step === 3 ? (
            <ReviewStep
              mode={mode}
              values={form.getValues()}
              data={data}
              routing={routing}
              onBack={() => setStep((current) => current - 1)}
              onSubmitted={setSubmitted}
            />
          ) : (
            <div className={styles.buttons}>
              {step > 0 ? (
                <Button variant="secondary" onClick={() => setStep((current) => current - 1)}>
                  Back
                </Button>
              ) : null}
              <Button type="submit">Continue</Button>
            </div>
          )}
        </form>
      </Card>
    </>
  );
}
