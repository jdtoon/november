import type { UseFormReturn } from "react-hook-form";
import { Checkbox, Select, TextInput } from "@november/ui";
import type { WizardData, WizardMode, WizardValues } from "../types";
import styles from "../Wizard.module.css";

type StepProps = {
  mode: WizardMode;
  form: UseFormReturn<WizardValues>;
  data: WizardData;
};

export function DetailsStep({ mode, form, data }: StepProps) {
  const { register, formState } = form;
  const errors = formState.errors;

  return (
    <div className={styles.stack}>
      <div className={styles.grid}>
        {mode !== "transfer" ? (
          <Select label="Purpose of payment" required placeholder="Choose a purpose" error={errors.purposeCode?.message} {...register("purposeCode")}>
            {data.purposeCodes.map((purpose) => (
              <option key={purpose.code} value={purpose.code}>
                {purpose.description}
              </option>
            ))}
          </Select>
        ) : null}
        <TextInput
          label="Payment reference"
          required
          maxLength={35}
          autoComplete="off"
          hint="Shown to the receiver, for example an invoice number. Up to 35 characters."
          error={errors.reference?.message}
          {...register("reference")}
        />
      </div>

      <div className={styles.stack}>
        <h3 className={styles.panelTitle}>Declarations</h3>
        <ul className={styles.declarations}>
          <li>The details I have entered are correct.</li>
          <li>I am authorised to capture payments for this company.</li>
          <li>I understand a second person must approve this payment before it is processed.</li>
        </ul>
        <Checkbox label="I accept these declarations" error={errors.declarationsAccepted?.message} {...register("declarationsAccepted")} />
      </div>
    </div>
  );
}
