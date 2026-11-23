# BFF API contract

The contract between the micro frontends and the BFF. The BFF serves the generated OpenAPI document at `/openapi/v1.json` and an interactive reference at `/scalar` in Development; this page is the human-readable summary.

## Conventions

- All endpoints are under `/api`. Health endpoints are `/health/live` and `/health/ready`.
- JSON, camelCase. Enums are serialised as strings.
- Properties whose value is `null` are omitted from responses rather than sent as `null`. Optional properties in the types below (`iban?`, `fx?`, `purpose?`) are therefore absent, never `null`.
- Dates are ISO 8601 UTC strings (`2026-11-23T10:15:00Z`).
- Amounts are JSON numbers in major units (`1250.50`). JPY has 0 decimals; every other currency has 2.
- Ids are opaque strings (`pay_7Hk2...`, `ben_...`, `acc_...`, `inc_...`, `fxq_...`).
- The browser never calls the BFF cross-origin. Everything goes through the shell's origin (`/api/...`), so there is no CORS.
- Authentication is an HttpOnly, SameSite=Strict session cookie set by the BFF.
- Every non-GET request must send the header `X-CSRF: 1`. Requests without it get `400`.
- `401` means not signed in. `403` means signed in but not allowed.
- Every endpoint under `/api` except `GET /api/session/personas`, `POST /api/session/login` and `POST /api/session/logout` requires a signed-in user. That includes the reference-data endpoints.

## Errors

Every error is an RFC 9457 `ProblemDetails` body with a machine-readable `code` extension:

```json
{
  "type": "https://november.example/problems/fx-quote-expired",
  "title": "The FX quote has expired",
  "status": 409,
  "detail": "Quote fxq_123 expired at 2026-11-23T10:16:00Z. Request a new quote.",
  "code": "fx_quote_expired",
  "traceId": "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01"
}
```

`traceId` is added to every error body. Quote it when reporting a problem.

| code | status | When |
| --- | --- | --- |
| `validation_failed` | 400 | Request failed validation. Includes `errors: { field: string[] }`. Field names are camelCased paths into the request body. |
| `csrf_header_missing` | 400 | Non-GET request without `X-CSRF: 1`. |
| `bad_request` | 400 | A route, query or body value could not be read (malformed JSON, `?status=Bogus`). |
| `unauthenticated` | 401 | Not signed in. |
| `not_found` | 404 | Unknown id, including an unknown FX quote id, or an unknown route. |
| `forbidden` | 403 | Missing role (for example a Capturer trying to approve). |
| `self_approval_forbidden` | 403 | Approver is the person who captured the payment. |
| `invalid_status` | 409 | Action not allowed in the current status. |
| `fx_quote_required` | 409 | Currencies differ but no `fxQuoteId` was sent. |
| `fx_quote_expired` | 409 | Quote is older than 60 seconds. |
| `fx_quote_used` | 409 | Quote was already consumed. |
| `fx_quote_mismatch` | 409 | Quote's currencies or amount don't match the request. |
| `insufficient_funds` | 422 | Available balance is too low. |
| `internal_error` | 500 | Anything unexpected. The body says nothing about the cause. |

An unauthenticated non-GET request without the CSRF header gets `401`, not `400`, because authorization runs before the CSRF check.

## Shared types (TypeScript notation)

```ts
type Role = "Capturer" | "Approver";
type CurrencyCode = "GBP" | "EUR" | "USD" | "ZAR" | "CHF" | "JPY";
type Money = { amount: number; currency: CurrencyCode };

type Session = {
  userId: string;
  displayName: string;
  roles: Role[];
  company: { id: string; name: string };
};

type Persona = { personaId: string; displayName: string; roles: Role[]; description: string };

type Account = {
  id: string;
  name: string;
  accountNumber: string;
  sortCode: string;
  iban: string;
  currency: CurrencyCode;
  balance: number;
  availableBalance: number;
};

type BankDetailsFormat = "Iban" | "AccountNumberSwift" | "AccountNumberRoutingSwift";
type Country = { code: string; name: string; currency: CurrencyCode; bankDetailsFormat: BankDetailsFormat };
type Currency = { code: CurrencyCode; name: string; decimals: number };
type PurposeDirection = "Outgoing" | "Incoming" | "Both";
type PurposeCode = { code: string; description: string; direction: PurposeDirection };

type Beneficiary = {
  id: string;
  name: string;
  country: string;
  currency: CurrencyCode;
  bankName: string;
  iban?: string;
  accountNumber?: string;
  routingNumber?: string;
  swiftBic: string;
  lastPaidAt?: string;
};

type FxQuote = {
  quoteId: string;
  sellCurrency: CurrencyCode;
  buyCurrency: CurrencyCode;
  rate: number;
  sellAmount: number;
  buyAmount: number;
  createdAt: string;
  expiresAt: string;
  validForSeconds: number;
};

type FixedSide = "Sell" | "Buy";

type PaymentType = "BeneficiaryPayment" | "InternationalTransfer";
type PaymentStatus = "PendingApproval" | "Processing" | "Completed" | "Rejected" | "Cancelled";

type PaymentSummary = {
  id: string;
  type: PaymentType;
  status: PaymentStatus;
  fromAccountName: string;
  payeeName: string;
  sendAmount: Money;
  receiveAmount: Money;
  reference: string;
  capturedBy: string;
  capturedAt: string;
  canApprove: boolean;
  canCancel: boolean;
};

type TimelineEvent = { at: string; event: string; by?: string; note?: string };

type PaymentDetail = PaymentSummary & {
  fromAccount: { id: string; name: string; currency: CurrencyCode };
  beneficiary?: Beneficiary;
  toAccount?: { id: string; name: string; currency: CurrencyCode };
  purpose?: PurposeCode;
  requiresFx: boolean;
  fixedSide: FixedSide;
  fx?: { rate: number; quoteId: string; acceptedBy: string; acceptedAt: string };
  rejectionReason?: string;
  timeline: TimelineEvent[];
};

type IncomingStatus = "ActionRequired" | "Processing" | "Credited" | "Cancelled";
type IncomingAction = "PurposeCode" | "FxAcceptance";

type IncomingPaymentSummary = {
  id: string;
  senderName: string;
  senderCountry: string;
  amount: Money;
  creditAccountName: string;
  status: IncomingStatus;
  receivedAt: string;
  actionsRequired: IncomingAction[];
};

type IncomingPaymentDetail = IncomingPaymentSummary & {
  senderBank: { name: string; swiftBic: string };
  senderReference: string;
  creditAccount: { id: string; name: string; currency: CurrencyCode };
  purpose?: PurposeCode;
  creditedAmount?: Money;
  fx?: { rate: number; quoteId: string; acceptedBy: string; acceptedAt: string };
  timeline: TimelineEvent[];
};

type Dashboard = {
  accounts: Account[];
  pendingApprovalCount: number;
  awaitingMyApprovalCount: number;
  incomingActionRequiredCount: number;
  recentPayments: PaymentSummary[];
};
```

## Endpoints

### Session (mock authentication)

| Method | Path | Body | Response | Notes |
| --- | --- | --- | --- | --- |
| GET | `/api/session` | | `Session` or 401 | |
| GET | `/api/session/personas` | | `Persona[]` | Anonymous. Mock mode only. |
| POST | `/api/session/login` | `{ personaId }` | `Session` | Sets the cookie. Mock mode only. |
| POST | `/api/session/logout` | | 204 | Clears the cookie. |

Persona ids: `alex` (Capturer), `sam` (Approver), `jordan` (Capturer and Approver).

### Dashboard

| Method | Path | Response | Notes |
| --- | --- | --- | --- |
| GET | `/api/dashboard` | `Dashboard` | One call that aggregates accounts, payments and incoming payments. A textbook BFF endpoint. |

### Reference data

| Method | Path | Response |
| --- | --- | --- |
| GET | `/api/reference/currencies` | `Currency[]` |
| GET | `/api/reference/countries` | `Country[]` (destination countries only; GB is the home country and is not listed) |
| GET | `/api/reference/purpose-codes?direction=Outgoing\|Incoming` | `PurposeCode[]` (matching direction plus `Both`) |

### Accounts and beneficiaries

| Method | Path | Response |
| --- | --- | --- |
| GET | `/api/accounts` | `Account[]` |
| GET | `/api/beneficiaries` | `Beneficiary[]` |
| GET | `/api/beneficiaries/{id}` | `Beneficiary` |

### FX

| Method | Path | Body | Response |
| --- | --- | --- | --- |
| POST | `/api/fx/quotes` | `{ sellCurrency, buyCurrency, amount, fixedSide: "Sell" \| "Buy" }` | 201 `FxQuote` |

`fixedSide: "Sell"` means `amount` is what leaves the account; `"Buy"` means `amount` is what the receiver gets.

### Outgoing payments and transfers

| Method | Path | Body | Response | Role |
| --- | --- | --- | --- | --- |
| POST | `/api/payments/beneficiary-payments` | `CreateBeneficiaryPayment` | 201 `PaymentDetail` | Capturer |
| POST | `/api/payments/international-transfers` | `CreateInternationalTransfer` | 201 `PaymentDetail` | Capturer |
| GET | `/api/payments?status=&type=` | | `PaymentSummary[]` (newest first) | any |
| GET | `/api/payments/{id}` | | `PaymentDetail` | any |
| POST | `/api/payments/{id}/approve` | `{ fxQuoteId?: string }` | `PaymentDetail` | Approver, not the capturer |
| POST | `/api/payments/{id}/reject` | `{ reason: string }` | `PaymentDetail` | Approver, not the capturer |
| POST | `/api/payments/{id}/cancel` | | `PaymentDetail` | The capturer only |

```ts
type CreateBeneficiaryPayment = {
  fromAccountId: string;
  beneficiaryId?: string;
  newBeneficiary?: {
    name: string;
    country: string;
    currency: CurrencyCode;
    bankName: string;
    iban?: string;
    accountNumber?: string;
    routingNumber?: string;
    swiftBic: string;
    saveForFuture: boolean;
  };
  amount: number;
  currency: CurrencyCode;
  purposeCode: string;
  reference: string;
  fxQuoteId?: string;
  declarationsAccepted: boolean;
};

type CreateInternationalTransfer = {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  currency: CurrencyCode;
  reference: string;
  fxQuoteId?: string;
  declarationsAccepted: boolean;
};
```

Rules:

- Exactly one of `beneficiaryId` and `newBeneficiary` must be set.
- `reference` is required and at most 35 characters.
- `currency` must be either the source account's currency (amount is what is sent, fixed sell) or the destination currency (amount is what is received, fixed buy).
- If the source and destination currencies differ, `fxQuoteId` is required and must match the pair, the fixed side and the amount.
- If they are the same, `fxQuoteId` must be omitted.
- `declarationsAccepted` must be `true`.
- `toAccountId` must differ from `fromAccountId`.
- Approving a payment where `requiresFx` is true needs a fresh `fxQuoteId`. Its sell currency must be the source account's currency, its buy currency must be the destination currency, and it must fix the same side and amount as the original capture.
- `fixedSide` on `PaymentDetail` records which side the capture fixed (`Sell` when `currency` was the source account's currency, `Buy` when it was the destination currency). Approvals reads it to request a matching fresh quote. It is present on every payment; for a same-currency payment it is `Sell` and has no effect.
- `canApprove` is true only when the current user has the Approver role, did not capture the payment, and the status is `PendingApproval`.
- `canCancel` is true only for the capturer while the status is `PendingApproval`.

### Incoming payments

| Method | Path | Body | Response |
| --- | --- | --- | --- |
| GET | `/api/incoming-payments?status=` | | `IncomingPaymentSummary[]` (newest first) |
| GET | `/api/incoming-payments/{id}` | | `IncomingPaymentDetail` |
| POST | `/api/incoming-payments/{id}/respond` | `{ purposeCode?: string, fxQuoteId?: string }` | `IncomingPaymentDetail` |

- `purposeCode` is required when `actionsRequired` contains `PurposeCode`.
- `fxQuoteId` is required when `actionsRequired` contains `FxAcceptance`. The quote sells the incoming currency, buys the credit account's currency, and fixes the sell side to the incoming amount.
- Allowed only while the status is `ActionRequired`. Any signed-in user may respond.
- After a successful response `actionsRequired` is `[]` and the status moves to `Processing`.
