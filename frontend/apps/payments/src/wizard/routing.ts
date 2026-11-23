import type { Account, Beneficiary, BankDetailsFormat, CurrencyCode, Country, FixedSide, FxQuoteRequest } from "@november/shared";
import type { WizardData, WizardMode, WizardValues } from "./types";

export type Routing = {
  fromAccount: Account | undefined;
  destinationCurrency: CurrencyCode | undefined;
  destinationLabel: string;
  beneficiary: Beneficiary | undefined;
  toAccount: Account | undefined;
  bankFormat: BankDetailsFormat | undefined;
  amountOptions: CurrencyCode[];
  requiresFx: boolean;
  fixedSide: FixedSide;
};

export function parseAmount(raw: string): number | null {
  const cleaned = raw.replace(/[\s,]/g, "");
  if (!/^\d+(\.\d+)?$/.test(cleaned)) return null;
  const value = Number(cleaned);
  return Number.isFinite(value) ? value : null;
}

export function bankFormatFor(countries: Country[], countryCode: string): BankDetailsFormat | undefined {
  return countries.find((country) => country.code === countryCode)?.bankDetailsFormat;
}

export function describeRouting(mode: WizardMode, values: WizardValues, data: WizardData): Routing {
  const fromAccount = data.accounts.find((account) => account.id === values.fromAccountId);
  const beneficiary = mode === "saved" ? data.beneficiaries.find((candidate) => candidate.id === values.beneficiaryId) : undefined;
  const toAccount = mode === "transfer" ? data.accounts.find((account) => account.id === values.toAccountId) : undefined;
  const bankFormat = mode === "new" ? bankFormatFor(data.countries, values.country) : undefined;

  let destinationCurrency: CurrencyCode | undefined;
  let destinationLabel = "";
  if (mode === "saved") {
    destinationCurrency = beneficiary?.currency;
    destinationLabel = beneficiary?.name ?? "";
  } else if (mode === "new") {
    destinationCurrency = (values.beneficiaryCurrency || undefined) as CurrencyCode | undefined;
    destinationLabel = values.name.trim();
  } else {
    destinationCurrency = toAccount?.currency;
    destinationLabel = toAccount?.name ?? "";
  }

  const amountOptions = [...new Set([fromAccount?.currency, destinationCurrency].filter((code): code is CurrencyCode => Boolean(code)))];
  const requiresFx = Boolean(fromAccount && destinationCurrency && fromAccount.currency !== destinationCurrency);
  const fixedSide: FixedSide = values.amountCurrency && values.amountCurrency !== fromAccount?.currency ? "Buy" : "Sell";

  return { fromAccount, destinationCurrency, destinationLabel, beneficiary, toAccount, bankFormat, amountOptions, requiresFx, fixedSide };
}

export function quoteRequestFor(routing: Routing, values: WizardValues): FxQuoteRequest | null {
  const amount = parseAmount(values.amount);
  if (!routing.requiresFx || !routing.fromAccount || !routing.destinationCurrency || amount === null) return null;
  return {
    sellCurrency: routing.fromAccount.currency,
    buyCurrency: routing.destinationCurrency,
    amount,
    fixedSide: routing.fixedSide,
  };
}
