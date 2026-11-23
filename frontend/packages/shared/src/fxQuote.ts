import type { FxQuote } from "./types";

type QuoteTiming = Pick<FxQuote, "createdAt" | "expiresAt" | "validForSeconds">;

export function quoteDeadline(quote: QuoteTiming, receivedAtMs: number): number {
  const lifetimeMs = Date.parse(quote.expiresAt) - Date.parse(quote.createdAt);
  const validForMs = Number.isFinite(lifetimeMs) && lifetimeMs > 0 ? lifetimeMs : quote.validForSeconds * 1000;
  return receivedAtMs + validForMs;
}

export function secondsRemaining(deadlineMs: number, nowMs: number): number {
  return Math.max(0, Math.ceil((deadlineMs - nowMs) / 1000));
}
