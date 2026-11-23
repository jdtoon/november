import type { Account, Beneficiary, Country, Currency, PurposeCode } from "@november/shared";

export type WizardMode = "saved" | "new" | "transfer";

export type WizardValues = {
  beneficiaryId: string;
  name: string;
  country: string;
  beneficiaryCurrency: string;
  bankName: string;
  iban: string;
  accountNumber: string;
  routingNumber: string;
  swiftBic: string;
  saveForFuture: boolean;
  fromAccountId: string;
  toAccountId: string;
  amount: string;
  amountCurrency: string;
  purposeCode: string;
  reference: string;
  declarationsAccepted: boolean;
};

export const emptyValues: WizardValues = {
  beneficiaryId: "",
  name: "",
  country: "",
  beneficiaryCurrency: "",
  bankName: "",
  iban: "",
  accountNumber: "",
  routingNumber: "",
  swiftBic: "",
  saveForFuture: false,
  fromAccountId: "",
  toAccountId: "",
  amount: "",
  amountCurrency: "",
  purposeCode: "",
  reference: "",
  declarationsAccepted: false,
};

export const modeTitles: Record<WizardMode, string> = {
  saved: "Pay a saved beneficiary",
  new: "Pay a new beneficiary",
  transfer: "International transfer",
};

export const stepLabels: Record<WizardMode, string[]> = {
  saved: ["Beneficiary", "Amount", "Details", "Review"],
  new: ["Beneficiary", "Amount", "Details", "Review"],
  transfer: ["Accounts", "Amount", "Details", "Review"],
};

export const stepFields: Record<WizardMode, Array<Array<keyof WizardValues>>> = {
  saved: [["beneficiaryId"], ["fromAccountId", "amountCurrency", "amount"], ["purposeCode", "reference", "declarationsAccepted"]],
  new: [
    ["name", "country", "beneficiaryCurrency", "bankName", "iban", "accountNumber", "routingNumber", "swiftBic"],
    ["fromAccountId", "amountCurrency", "amount"],
    ["purposeCode", "reference", "declarationsAccepted"],
  ],
  transfer: [["fromAccountId", "toAccountId"], ["amountCurrency", "amount"], ["reference", "declarationsAccepted"]],
};

export type WizardData = {
  accounts: Account[];
  beneficiaries: Beneficiary[];
  countries: Country[];
  currencies: Currency[];
  purposeCodes: PurposeCode[];
};
