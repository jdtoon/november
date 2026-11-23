import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "./api/endpoints";
import { toApiError } from "./api/client";
import type { ApiError } from "./api/client";
import { quoteDeadline, secondsRemaining } from "./fxQuote";
import type { FxQuote, FxQuoteRequest } from "./types";

type Held = { quote: FxQuote; deadline: number };

export type FxQuoteState = {
  quote: FxQuote | null;
  secondsLeft: number;
  expired: boolean;
  pending: boolean;
  error: ApiError | null;
  request: (request: FxQuoteRequest) => Promise<FxQuote | undefined>;
  refresh: () => Promise<FxQuote | undefined>;
  clear: () => void;
  markExpired: () => void;
};

export function useFxQuote(): FxQuoteState {
  const [held, setHeld] = useState<Held | null>(null);
  const [now, setNow] = useState(() => Date.now());
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const lastRequest = useRef<FxQuoteRequest | null>(null);
  const ticket = useRef(0);

  useEffect(() => {
    if (!held) return;
    setNow(Date.now());
    const timer = window.setInterval(() => {
      const current = Date.now();
      setNow(current);
      if (current >= held.deadline) window.clearInterval(timer);
    }, 250);
    return () => window.clearInterval(timer);
  }, [held]);

  const request = useCallback(async (next: FxQuoteRequest) => {
    lastRequest.current = next;
    const mine = ++ticket.current;
    setPending(true);
    setError(null);
    try {
      const quote = await api.createFxQuote(next);
      if (mine !== ticket.current) return undefined;
      const receivedAt = Date.now();
      setNow(receivedAt);
      setHeld({ quote, deadline: quoteDeadline(quote, receivedAt) });
      return quote;
    } catch (caught) {
      if (mine === ticket.current) {
        setHeld(null);
        setError(toApiError(caught));
      }
      return undefined;
    } finally {
      if (mine === ticket.current) setPending(false);
    }
  }, []);

  const refresh = useCallback(async () => {
    if (!lastRequest.current) return undefined;
    return request(lastRequest.current);
  }, [request]);

  const clear = useCallback(() => {
    ticket.current++;
    setHeld(null);
    setError(null);
    setPending(false);
  }, []);

  const markExpired = useCallback(() => {
    setHeld((current) => (current ? { ...current, deadline: 0 } : current));
  }, []);

  const secondsLeft = held ? secondsRemaining(held.deadline, now) : 0;

  return {
    quote: held?.quote ?? null,
    secondsLeft,
    expired: held !== null && secondsLeft === 0,
    pending,
    error,
    request,
    refresh,
    clear,
    markExpired,
  };
}
