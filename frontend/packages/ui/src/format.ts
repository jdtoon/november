export function currencyDecimals(currency: string): number {
  return currency === "JPY" ? 0 : 2;
}

export function formatMoney(amount: number, currency: string, locale = "en-GB"): string {
  const decimals = currencyDecimals(currency);
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(amount);
}

export function formatRate(rate: number): string {
  return new Intl.NumberFormat("en-GB", { minimumFractionDigits: 4, maximumFractionDigits: 6 }).format(rate);
}

export function formatDateTime(iso: string, locale = "en-GB"): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return new Intl.DateTimeFormat(locale, { dateStyle: "medium", timeStyle: "short" }).format(date);
}

export function humanize(value: string): string {
  const spaced = value.replace(/([a-z0-9])([A-Z])/g, "$1 $2").toLowerCase();
  return spaced.charAt(0).toUpperCase() + spaced.slice(1);
}
