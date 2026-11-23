import { queryOptions } from "@tanstack/react-query";
import { api } from "./endpoints";
import { ApiError } from "./client";
import { queryKeys } from "./queryKeys";
import type { IncomingFilter, PaymentFilter, Session } from "../types";

const referenceStaleTime = 5 * 60 * 1000;

export const queries = {
  session: () =>
    queryOptions({
      queryKey: queryKeys.session,
      queryFn: async (): Promise<Session | null> => {
        try {
          return await api.getSession();
        } catch (error) {
          if (error instanceof ApiError && error.status === 401) return null;
          throw error;
        }
      },
      staleTime: 60_000,
    }),
  personas: () => queryOptions({ queryKey: queryKeys.personas, queryFn: api.listPersonas, staleTime: referenceStaleTime }),
  dashboard: () => queryOptions({ queryKey: queryKeys.dashboard, queryFn: api.getDashboard }),
  currencies: () => queryOptions({ queryKey: queryKeys.currencies, queryFn: api.listCurrencies, staleTime: referenceStaleTime }),
  countries: () => queryOptions({ queryKey: queryKeys.countries, queryFn: api.listCountries, staleTime: referenceStaleTime }),
  purposeCodes: (direction: "Outgoing" | "Incoming") =>
    queryOptions({
      queryKey: queryKeys.purposeCodes(direction),
      queryFn: () => api.listPurposeCodes(direction),
      staleTime: referenceStaleTime,
    }),
  accounts: () => queryOptions({ queryKey: queryKeys.accounts, queryFn: api.listAccounts }),
  beneficiaries: () => queryOptions({ queryKey: queryKeys.beneficiaries, queryFn: api.listBeneficiaries }),
  payments: (filter: PaymentFilter = {}) =>
    queryOptions({ queryKey: queryKeys.payments.list(filter), queryFn: () => api.listPayments(filter) }),
  payment: (id: string) => queryOptions({ queryKey: queryKeys.payments.detail(id), queryFn: () => api.getPayment(id) }),
  incomingPayments: (filter: IncomingFilter = {}) =>
    queryOptions({ queryKey: queryKeys.incoming.list(filter), queryFn: () => api.listIncomingPayments(filter) }),
  incomingPayment: (id: string) =>
    queryOptions({ queryKey: queryKeys.incoming.detail(id), queryFn: () => api.getIncomingPayment(id) }),
};
