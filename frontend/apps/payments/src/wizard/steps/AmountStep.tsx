import { useEffect } from "react";
import { useWatch } from "react-hook-form";
import type { UseFormReturn } from "react-hook-form";
import { Select, TextInput, formatMoney } from "@november/ui";
import type { CurrencyCode } from "@november/shared";
import type { Routing } from "../routing";
import type { WizardData, WizardMode, WizardValues } from "../types";
import styles from "../Wizard.module.css";

type StepProps = {
  mode: WizardMode;
  form: UseFormReturn<WizardValues>;
  data: WizardData;
  routing: Routing;
};

export function AmountStep({ mode, form, data, routing }: StepProps) {
  const { register, setValue, control, formState } = form;
  const errors = formState.errors;
  const amountCurrency = useWatch({ control, name: "amountCurrency" });
  const options = routing.amountOptions;
  const optionsKey = options.join(",");

  useEffect(() => {
    const first = options[0];
    if (first && !options.includes(amountCurrency as CurrencyCode)) setValue("amountCurrency", first);
  }, [optionsKey, amountCurrency, options, setValue]);

  const recipient = routing.destinationLabel || "the recipient";
  const fixedHint = !routing.requiresFx
    ? undefined
    : routing.fixedSide === "Sell"
      ? "This is exactly what leaves your account. The recipient gets the converted amount."
      : `This is exactly what ${recipient} receives. The amount leaving your account is worked out from the rate.`;

  return (
    <div className={styles.stack}>
      {mode !== "transfer" ? (
        <Select label="Pay from" required error={errors.fromAccountId?.message} {...register("fromAccountId")}>
          {data.accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name} ({account.currency}), {formatMoney(account.availableBalance, account.currency)} available
            </option>
          ))}
        </Select>
      ) : null}
      <div className={styles.grid}>
        <TextInput label="Amount" required inputMode="decimal" autoComplete="off" error={errors.amount?.message} {...register("amount")} />
        <Select label="Amount is in" required hint={fixedHint} error={errors.amountCurrency?.message} {...register("amountCurrency")}>
          {options.map((code) => (
            <option key={code} value={code}>
              {code}
              {routing.requiresFx ? (code === routing.fromAccount?.currency ? " (amount leaving your account)" : ` (amount ${recipient} receives)`) : ""}
            </option>
          ))}
        </Select>
      </div>
    </div>
  );
}
