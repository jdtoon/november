import { apiFetch } from "./client";
import type {
  Account,
  Beneficiary,
  CreateBeneficiaryPayment,
  CreateInternationalTransfer,
  Country,
  Currency,
  Dashboard,
  FxQuote,
  FxQuoteRequest,
  IncomingFilter,
  IncomingPaymentDetail,
  IncomingPaymentSummary,
  PaymentDetail,
  PaymentFilter,
  PaymentSummary,
  Persona,
  PurposeCode,
  PurposeDirection,
  Session,
} from "../types";

export const api = {
  getSession: () => apiFetch<Session>("/api/session", { suppressSessionExpired: true }),
  listPersonas: () => apiFetch<Persona[]>("/api/session/personas", { suppressSessionExpired: true }),
  login: (personaId: string) =>
    apiFetch<Session>("/api/session/login", { method: "POST", body: { personaId }, suppressSessionExpired: true }),
  logout: () => apiFetch<void>("/api/session/logout", { method: "POST", suppressSessionExpired: true }),

  getDashboard: () => apiFetch<Dashboard>("/api/dashboard"),

  listCurrencies: () => apiFetch<Currency[]>("/api/reference/currencies"),
  listCountries: () => apiFetch<Country[]>("/api/reference/countries"),
  listPurposeCodes: (direction: Exclude<PurposeDirection, "Both">) =>
    apiFetch<PurposeCode[]>("/api/reference/purpose-codes", { query: { direction } }),

  listAccounts: () => apiFetch<Account[]>("/api/accounts"),
  listBeneficiaries: () => apiFetch<Beneficiary[]>("/api/beneficiaries"),

  createFxQuote: (request: FxQuoteRequest) => apiFetch<FxQuote>("/api/fx/quotes", { method: "POST", body: request }),

  listPayments: (filter: PaymentFilter = {}) => apiFetch<PaymentSummary[]>("/api/payments", { query: filter }),
  getPayment: (id: string) => apiFetch<PaymentDetail>(`/api/payments/${encodeURIComponent(id)}`),
  createBeneficiaryPayment: (body: CreateBeneficiaryPayment) =>
    apiFetch<PaymentDetail>("/api/payments/beneficiary-payments", { method: "POST", body }),
  createInternationalTransfer: (body: CreateInternationalTransfer) =>
    apiFetch<PaymentDetail>("/api/payments/international-transfers", { method: "POST", body }),
  approvePayment: (id: string, fxQuoteId?: string) =>
    apiFetch<PaymentDetail>(`/api/payments/${encodeURIComponent(id)}/approve`, { method: "POST", body: { fxQuoteId } }),
  rejectPayment: (id: string, reason: string) =>
    apiFetch<PaymentDetail>(`/api/payments/${encodeURIComponent(id)}/reject`, { method: "POST", body: { reason } }),
  cancelPayment: (id: string) => apiFetch<PaymentDetail>(`/api/payments/${encodeURIComponent(id)}/cancel`, { method: "POST" }),

  listIncomingPayments: (filter: IncomingFilter = {}) =>
    apiFetch<IncomingPaymentSummary[]>("/api/incoming-payments", { query: filter }),
  getIncomingPayment: (id: string) => apiFetch<IncomingPaymentDetail>(`/api/incoming-payments/${encodeURIComponent(id)}`),
  respondToIncomingPayment: (id: string, body: { purposeCode?: string; fxQuoteId?: string }) =>
    apiFetch<IncomingPaymentDetail>(`/api/incoming-payments/${encodeURIComponent(id)}/respond`, { method: "POST", body }),
};
