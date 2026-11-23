export type Role = "Capturer" | "Approver";
export type CurrencyCode = "GBP" | "EUR" | "USD" | "ZAR" | "CHF" | "JPY";
export type Money = { amount: number; currency: CurrencyCode };

export type Session = {
  userId: string;
  displayName: string;
  roles: Role[];
  company: { id: string; name: string };
};

export type Persona = { personaId: string; displayName: string; roles: Role[]; description: string };

export type Account = {
  id: string;
  name: string;
  accountNumber: string;
  sortCode: string;
  iban: string;
  currency: CurrencyCode;
  balance: number;
  availableBalance: number;
};

export type BankDetailsFormat = "Iban" | "AccountNumberSwift" | "AccountNumberRoutingSwift";
export type Country = { code: string; name: string; currency: CurrencyCode; bankDetailsFormat: BankDetailsFormat };
export type Currency = { code: CurrencyCode; name: string; decimals: number };
export type PurposeDirection = "Outgoing" | "Incoming" | "Both";
export type PurposeCode = { code: string; description: string; direction: PurposeDirection };

export type Beneficiary = {
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

export type FixedSide = "Sell" | "Buy";

export type FxQuoteRequest = {
  sellCurrency: CurrencyCode;
  buyCurrency: CurrencyCode;
  amount: number;
  fixedSide: FixedSide;
};

export type FxQuote = {
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

export type PaymentType = "BeneficiaryPayment" | "InternationalTransfer";
export type PaymentStatus = "PendingApproval" | "Processing" | "Completed" | "Rejected" | "Cancelled";

export type PaymentSummary = {
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

export type TimelineEvent = { at: string; event: string; by?: string; note?: string };

export type AccountRef = { id: string; name: string; currency: CurrencyCode };
export type FxAcceptance = { rate: number; quoteId: string; acceptedBy: string; acceptedAt: string };

export type PaymentDetail = PaymentSummary & {
  fromAccount: AccountRef;
  beneficiary?: Beneficiary;
  toAccount?: AccountRef;
  purpose?: PurposeCode;
  requiresFx: boolean;
  fixedSide: FixedSide;
  fx?: FxAcceptance;
  rejectionReason?: string;
  timeline: TimelineEvent[];
};

export type IncomingStatus = "ActionRequired" | "Processing" | "Credited" | "Cancelled";
export type IncomingAction = "PurposeCode" | "FxAcceptance";

export type IncomingPaymentSummary = {
  id: string;
  senderName: string;
  senderCountry: string;
  amount: Money;
  creditAccountName: string;
  status: IncomingStatus;
  receivedAt: string;
  actionsRequired: IncomingAction[];
};

export type IncomingPaymentDetail = IncomingPaymentSummary & {
  senderBank: { name: string; swiftBic: string };
  senderReference: string;
  creditAccount: AccountRef;
  purpose?: PurposeCode;
  creditedAmount?: Money;
  fx?: FxAcceptance;
  timeline: TimelineEvent[];
};

export type Dashboard = {
  accounts: Account[];
  pendingApprovalCount: number;
  awaitingMyApprovalCount: number;
  incomingActionRequiredCount: number;
  recentPayments: PaymentSummary[];
};

export type NewBeneficiaryInput = {
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

export type CreateBeneficiaryPayment = {
  fromAccountId: string;
  beneficiaryId?: string;
  newBeneficiary?: NewBeneficiaryInput;
  amount: number;
  currency: CurrencyCode;
  purposeCode: string;
  reference: string;
  fxQuoteId?: string;
  declarationsAccepted: boolean;
};

export type CreateInternationalTransfer = {
  fromAccountId: string;
  toAccountId: string;
  amount: number;
  currency: CurrencyCode;
  reference: string;
  fxQuoteId?: string;
  declarationsAccepted: boolean;
};

export type PaymentFilter = { status?: PaymentStatus; type?: PaymentType };
export type IncomingFilter = { status?: IncomingStatus };

export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

export const currencyCodes: CurrencyCode[] = ["GBP", "EUR", "USD", "ZAR", "CHF", "JPY"];
