import type { PaymentStatus } from "./types";

export type NovemberEvents = {
  "november:session-expired": undefined;
  "november:payment-submitted": { paymentId: string };
  "november:payment-decided": { paymentId: string; outcome: Extract<PaymentStatus, "Processing" | "Rejected" | "Cancelled"> };
  "november:incoming-responded": { incomingId: string };
};

export type NovemberEventName = keyof NovemberEvents;

export function emit<K extends NovemberEventName>(name: K, ...detail: NovemberEvents[K] extends undefined ? [] : [NovemberEvents[K]]): void {
  window.dispatchEvent(new CustomEvent(name, { detail: detail[0] }));
}

export function on<K extends NovemberEventName>(name: K, handler: (detail: NovemberEvents[K]) => void): () => void {
  const listener = (event: Event) => handler((event as CustomEvent<NovemberEvents[K]>).detail);
  window.addEventListener(name, listener);
  return () => window.removeEventListener(name, listener);
}
