import { describe, expect, it } from "vitest";
import { quoteDeadline, secondsRemaining } from "./fxQuote";

const quote = {
  createdAt: "2026-11-23T10:15:00Z",
  expiresAt: "2026-11-23T10:16:00Z",
  validForSeconds: 60,
};

describe("quote countdown", () => {
  it("counts down from the quote lifetime, not from the client clock", () => {
    const receivedAt = Date.parse("2026-11-23T09:00:00Z");
    const deadline = quoteDeadline(quote, receivedAt);

    expect(secondsRemaining(deadline, receivedAt)).toBe(60);
    expect(secondsRemaining(deadline, receivedAt + 12_300)).toBe(48);
  });

  it("expires at zero and never goes negative", () => {
    const receivedAt = 1_000_000;
    const deadline = quoteDeadline(quote, receivedAt);

    expect(secondsRemaining(deadline, receivedAt + 60_000)).toBe(0);
    expect(secondsRemaining(deadline, receivedAt + 90_000)).toBe(0);
  });

  it("falls back to validForSeconds when the timestamps are unusable", () => {
    const deadline = quoteDeadline({ createdAt: "nope", expiresAt: "nope", validForSeconds: 30 }, 0);

    expect(secondsRemaining(deadline, 0)).toBe(30);
  });
});
