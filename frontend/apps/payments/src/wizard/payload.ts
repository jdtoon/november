import type { CreateBeneficiaryPayment, CreateInternationalTransfer, CurrencyCode, NewBeneficiaryInput } from "@november/shared";
import { parseAmount } from "./routing";
import type { Routing } from "./routing";
import type { WizardMode, WizardValues } from "./types";
import { normaliseIban } from "./validation";

function newBeneficiaryFrom(values: WizardValues, routing: Routing): NewBeneficiaryInput {
  const beneficiary: NewBeneficiaryInput = {
    name: values.name.trim(),
    country: values.country,
    currency: values.beneficiaryCurrency as CurrencyCode,
    bankName: values.bankName.trim(),
    swiftBic: values.swiftBic.trim().toUpperCase(),
    saveForFuture: values.saveForFuture,
  };
  if (routing.bankFormat === "Iban") beneficiary.iban = normaliseIban(values.iban);
  if (routing.bankFormat === "AccountNumberSwift" || routing.bankFormat === "AccountNumberRoutingSwift") {
    beneficiary.accountNumber = values.accountNumber.replace(/\s+/g, "");
  }
  if (routing.bankFormat === "AccountNumberRoutingSwift") beneficiary.routingNumber = values.routingNumber.trim();
  return beneficiary;
}

export function buildPayload(
  mode: WizardMode,
  values: WizardValues,
  routing: Routing,
  fxQuoteId: string | undefined,
): CreateBeneficiaryPayment | CreateInternationalTransfer {
  const common = {
    fromAccountId: values.fromAccountId,
    amount: parseAmount(values.amount) ?? 0,
    currency: values.amountCurrency as CurrencyCode,
    reference: values.reference.trim(),
    declarationsAccepted: values.declarationsAccepted,
    ...(fxQuoteId ? { fxQuoteId } : {}),
  };

  if (mode === "transfer") {
    return { ...common, toAccountId: values.toAccountId };
  }

  return {
    ...common,
    purposeCode: values.purposeCode,
    ...(mode === "saved" ? { beneficiaryId: values.beneficiaryId } : { newBeneficiary: newBeneficiaryFrom(values, routing) }),
  };
}
