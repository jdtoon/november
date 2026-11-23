# ADR 0005: Maker-checker and FX re-quote

## Status

Accepted

## Context

Two business rules shape the payments flow ([`docs/domain.md`](../domain.md)):

1. **Maker-checker (four-eyes).** One person captures an outgoing payment and a different person approves it. No one can do both for the same payment.
2. **Time-limited FX quotes.** A quote is valid for 60 seconds and can be used once. Its currency pair and amount must match the request that uses it.

These combine awkwardly. The capturer accepts a rate when they submit, but the approver acts minutes or hours later, when that quote expired long ago and the market has moved. Someone has to decide which rate the bank honours, and where each rule lives, given that this is a BFF in front of platforms that would own them in reality.

## Decision

**The approver commits to the rate.** The capturer's quote proves the capturer saw a price and fixes which side of the trade is fixed (send exactly X, or they receive exactly Y). On approval the approver must supply a fresh quote for the same currency pair, fixed side and amount. That quote's rate is the one used. If the payment's other side changes as a result, the stored amounts and the reserved funds are updated.

**The platform owns the rules.** The in-memory payments and FX services (the stand-ins for real platforms) enforce:

- status transitions (only `PendingApproval` can be approved, rejected or cancelled);
- four-eyes (approve and reject fail with `self_approval_forbidden` for the capturer; only the capturer can cancel);
- quote redemption: not found, already used, expired, or mismatched (currency pair, fixed side, amount) are rejected with distinct codes, and redemption is atomic so a quote cannot be consumed twice;
- funds: the send amount is reserved at capture, released on reject or cancel, and settled when processing completes.

**The BFF orchestrates and pre-checks.** The handlers translate the UI's `amount` and `currency` into a fixed side, call the platform, pre-check four-eyes before any quote is spent (to give a clear error early), and compute `canApprove` and `canCancel` per viewer so the UI never re-implements the rules.

The detailed flow and code are in [`docs/bff/README.md`](../bff/README.md#8-fx-quotes-and-four-eyes).

## Consequences

Good:

- The rate that is booked is the rate the approver saw and accepted, which is the property a checker needs.
- The rules are enforced where a real system would enforce them, so swapping the fakes for real HTTP clients does not move business logic out of the BFF.
- Clients cannot bypass four-eyes by calling the BFF differently: the platform refuses.
- The UI is simple. It shows or hides buttons from `canApprove` and `canCancel`.

Costs and risks:

- The four-eyes check exists in two places (the platform and the BFF pre-check). This is intended duplication, but the two must keep the same meaning.
- With a fixed-buy payment, the send amount can change at approval. If the account can no longer afford it, approval fails with `insufficient_funds`, and the approver's quote has already been spent, so they must request another. A real platform might redeem the quote only after the funds check.
- The approver needs a quote at the moment of approval, so the approval screen needs a quote step with a 60-second countdown, the same as capture.
- A capture-time quote has no lasting effect on the final rate. That surprises some users, and the UI should say the rate shown at capture is indicative.
- No payment limits, cut-off times or fees are modelled (assumption 4 in the domain primer).
