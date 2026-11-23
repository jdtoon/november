import { useWatch } from "react-hook-form";
import type { UseFormReturn } from "react-hook-form";
import { DescriptionList, Select, TextInput, Checkbox, formatMoney } from "@november/ui";
import type { Routing } from "../routing";
import type { WizardData, WizardMode, WizardValues } from "../types";
import styles from "../Wizard.module.css";

type StepProps = {
  mode: WizardMode;
  form: UseFormReturn<WizardValues>;
  data: WizardData;
  routing: Routing;
};

export function RecipientStep({ mode, form, data, routing }: StepProps) {
  const { register, setValue, control, formState } = form;
  const errors = formState.errors;
  const country = useWatch({ control, name: "country" });
  const format = data.countries.find((candidate) => candidate.code === country)?.bankDetailsFormat;

  if (mode === "saved") {
    const beneficiary = routing.beneficiary;
    return (
      <div className={styles.stack}>
        <Select label="Beneficiary" required placeholder="Choose a beneficiary" error={errors.beneficiaryId?.message} {...register("beneficiaryId")}>
          {data.beneficiaries.map((item) => (
            <option key={item.id} value={item.id}>
              {item.name} ({item.country}, {item.currency})
            </option>
          ))}
        </Select>
        {beneficiary ? (
          <div className={styles.preview}>
            <DescriptionList
              items={[
                { term: "Bank", description: beneficiary.bankName },
                { term: "Paid in", description: beneficiary.currency },
                { term: beneficiary.iban ? "IBAN" : "Account number", description: beneficiary.iban ?? beneficiary.accountNumber ?? "" },
                { term: "SWIFT / BIC", description: beneficiary.swiftBic },
              ]}
            />
          </div>
        ) : null}
      </div>
    );
  }

  if (mode === "transfer") {
    return (
      <div className={styles.grid}>
        <Select label="From account" required placeholder="Choose an account" error={errors.fromAccountId?.message} {...register("fromAccountId")}>
          {data.accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name} ({account.currency}), {formatMoney(account.availableBalance, account.currency)} available
            </option>
          ))}
        </Select>
        <Select label="To account" required placeholder="Choose an account" error={errors.toAccountId?.message} {...register("toAccountId")}>
          {data.accounts.map((account) => (
            <option key={account.id} value={account.id}>
              {account.name} ({account.currency})
            </option>
          ))}
        </Select>
      </div>
    );
  }

  return (
    <div className={styles.stack}>
      <div className={styles.grid}>
        <TextInput label="Beneficiary name" required autoComplete="off" error={errors.name?.message} {...register("name")} />
        <Select
          label="Country"
          required
          placeholder="Choose a country"
          error={errors.country?.message}
          {...register("country", {
            onChange: (event) => {
              const match = data.countries.find((candidate) => candidate.code === event.target.value);
              setValue("beneficiaryCurrency", match?.currency ?? "");
            },
          })}
        >
          {[...data.countries]
            .sort((a, b) => a.name.localeCompare(b.name))
            .map((item) => (
              <option key={item.code} value={item.code}>
                {item.name}
              </option>
            ))}
        </Select>
        <Select
          label="Currency they are paid in"
          required
          placeholder="Choose a currency"
          error={errors.beneficiaryCurrency?.message}
          {...register("beneficiaryCurrency")}
        >
          {data.currencies.map((item) => (
            <option key={item.code} value={item.code}>
              {item.code}, {item.name}
            </option>
          ))}
        </Select>
        <TextInput label="Bank name" required autoComplete="off" error={errors.bankName?.message} {...register("bankName")} />
      </div>

      {format ? (
        <div className={styles.grid}>
          {format === "Iban" ? (
            <TextInput label="IBAN" required autoComplete="off" spellCheck={false} error={errors.iban?.message} {...register("iban")} />
          ) : (
            <TextInput label="Account number" required autoComplete="off" inputMode="numeric" error={errors.accountNumber?.message} {...register("accountNumber")} />
          )}
          {format === "AccountNumberRoutingSwift" ? (
            <TextInput label="Routing number (ABA)" required autoComplete="off" inputMode="numeric" hint="Nine digits" error={errors.routingNumber?.message} {...register("routingNumber")} />
          ) : null}
          <TextInput label="SWIFT / BIC" required autoComplete="off" spellCheck={false} hint="8 or 11 characters" error={errors.swiftBic?.message} {...register("swiftBic")} />
        </div>
      ) : (
        <p>Choose a country to see which bank details are needed.</p>
      )}

      <Checkbox label="Save this beneficiary for future payments" {...register("saveForFuture")} />
    </div>
  );
}
