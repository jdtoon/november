import type { IncomingFilter, PaymentFilter } from "../types";

export const queryKeys = {
  session: ["session"] as const,
  personas: ["session", "personas"] as const,
  dashboard: ["dashboard"] as const,
  currencies: ["reference", "currencies"] as const,
  countries: ["reference", "countries"] as const,
  purposeCodes: (direction: string) => ["reference", "purpose-codes", direction] as const,
  accounts: ["accounts"] as const,
  beneficiaries: ["beneficiaries"] as const,
  payments: {
    all: ["payments"] as const,
    list: (filter: PaymentFilter) => ["payments", "list", filter] as const,
    detail: (id: string) => ["payments", "detail", id] as const,
  },
  incoming: {
    all: ["incoming-payments"] as const,
    list: (filter: IncomingFilter) => ["incoming-payments", "list", filter] as const,
    detail: (id: string) => ["incoming-payments", "detail", id] as const,
  },
};
