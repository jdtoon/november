import { z } from "zod";
import { currencyDecimals } from "@november/ui";
import type { Country } from "@november/shared";
import { bankFormatFor, parseAmount } from "./routing";
import type { WizardMode, WizardValues } from "./types";

export function normaliseIban(raw: string): string {
  return raw.replace(/\s+/g, "").toUpperCase();
}

export function isValidIban(raw: string): boolean {
  const iban = normaliseIban(raw);
  if (!/^[A-Z]{2}\d{2}[A-Z0-9]{11,30}$/.test(iban)) return false;
  const rearranged = iban.slice(4) + iban.slice(0, 4);
  let remainder = 0;
  for (const char of rearranged) {
    const value = char >= "A" ? char.charCodeAt(0) - 55 : Number(char);
    remainder = Number(`${remainder}${value}`) % 97;
  }
  return remainder === 1;
}

const swiftPattern = /^[A-Za-z]{4}[A-Za-z]{2}[A-Za-z0-9]{2}([A-Za-z0-9]{3})?$/;

export function buildSchema(mode: WizardMode, countries: Country[]) {
  return z
    .object({
      beneficiaryId: z.string(),
      name: z.string(),
      country: z.string(),
      beneficiaryCurrency: z.string(),
      bankName: z.string(),
      iban: z.string(),
      accountNumber: z.string(),
      routingNumber: z.string(),
      swiftBic: z.string(),
      saveForFuture: z.boolean(),
      fromAccountId: z.string(),
      toAccountId: z.string(),
      amount: z.string(),
      amountCurrency: z.string(),
      purposeCode: z.string(),
      reference: z.string(),
      declarationsAccepted: z.boolean(),
    })
    .superRefine((values: WizardValues, ctx) => {
      const fail = (path: keyof WizardValues, message: string) => ctx.addIssue({ code: "custom", path: [path], message });

      if (mode === "saved" && !values.beneficiaryId) fail("beneficiaryId", "Choose a beneficiary");

      if (mode === "new") {
        if (!values.name.trim()) fail("name", "Enter the beneficiary's name");
        if (!values.country) fail("country", "Choose the beneficiary's country");
        if (!values.beneficiaryCurrency) fail("beneficiaryCurrency", "Choose the currency they are paid in");
        if (!values.bankName.trim()) fail("bankName", "Enter the beneficiary's bank name");

        const format = bankFormatFor(countries, values.country);
        if (format === "Iban") {
          if (!isValidIban(values.iban)) fail("iban", "Enter a valid IBAN");
          else if (!normaliseIban(values.iban).startsWith(values.country)) fail("iban", `An IBAN for this country starts with ${values.country}`);
        }
        if (format === "AccountNumberSwift" || format === "AccountNumberRoutingSwift") {
          if (!/^\d{4,17}$/.test(values.accountNumber.replace(/\s+/g, ""))) fail("accountNumber", "Enter an account number of 4 to 17 digits");
        }
        if (format === "AccountNumberRoutingSwift" && !/^\d{9}$/.test(values.routingNumber.trim())) {
          fail("routingNumber", "Enter the 9-digit routing number");
        }
        if (values.country && !swiftPattern.test(values.swiftBic.trim())) fail("swiftBic", "Enter a valid SWIFT/BIC (8 or 11 characters)");
      }

      if (!values.fromAccountId) fail("fromAccountId", "Choose the account to pay from");

      if (mode === "transfer") {
        if (!values.toAccountId) fail("toAccountId", "Choose the account to transfer to");
        else if (values.toAccountId === values.fromAccountId) fail("toAccountId", "Choose a different account from the one you are paying from");
      }

      if (!values.amountCurrency) fail("amountCurrency", "Choose the currency of the amount");
      const amount = parseAmount(values.amount);
      if (amount === null || amount <= 0) {
        fail("amount", "Enter an amount greater than zero");
      } else if (values.amountCurrency) {
        const decimals = currencyDecimals(values.amountCurrency);
        const places = values.amount.replace(/[\s,]/g, "").split(".")[1]?.length ?? 0;
        if (places > decimals) fail("amount", decimals === 0 ? `${values.amountCurrency} amounts have no decimal places` : `Use at most ${decimals} decimal places`);
      }

      if (mode !== "transfer" && !values.purposeCode) fail("purposeCode", "Choose a purpose");

      const reference = values.reference.trim();
      if (!reference) fail("reference", "Enter a payment reference");
      else if (reference.length > 35) fail("reference", "Keep the reference to 35 characters or fewer");

      if (!values.declarationsAccepted) fail("declarationsAccepted", "Accept the declarations to continue");
    });
}
