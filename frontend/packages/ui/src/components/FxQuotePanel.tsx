import { formatMoney, formatRate } from "../format";
import { Alert } from "./Alert";
import { Button } from "./Button";
import { cx } from "./cx";
import { Spinner } from "./Spinner";
import { VisuallyHidden } from "./VisuallyHidden";
import styles from "./FxQuotePanel.module.css";

export type FxQuoteView = {
  sellCurrency: string;
  buyCurrency: string;
  rate: number;
  sellAmount: number;
  buyAmount: number;
};

export type FxQuotePanelProps = {
  quote: FxQuoteView | null;
  secondsLeft: number;
  validForSeconds?: number;
  expired: boolean;
  loading?: boolean;
  error?: string;
  onRefresh: () => void;
  sellLabel?: string;
  buyLabel?: string;
};

function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${String(seconds).padStart(2, "0")}`;
}

function announcementFor(expired: boolean, secondsLeft: number, validForSeconds: number): string {
  if (expired) return "The rate has expired. Get a new quote to continue.";
  if (secondsLeft <= 10) return "The rate expires in less than 10 seconds.";
  if (secondsLeft <= 30) return "The rate expires in less than 30 seconds.";
  return `Rate locked. It is valid for ${validForSeconds} seconds.`;
}

export function FxQuotePanel({
  quote,
  secondsLeft,
  validForSeconds = 60,
  expired,
  loading = false,
  error,
  onRefresh,
  sellLabel = "You send",
  buyLabel = "They receive",
}: FxQuotePanelProps) {
  if (!quote) {
    return (
      <div className={styles.panel}>
        {loading ? <Spinner label="Getting an exchange rate" /> : null}
        {error ? (
          <Alert tone="danger" title="Could not get an exchange rate" actions={<Button variant="secondary" size="sm" onClick={onRefresh}>Try again</Button>}>
            {error}
          </Alert>
        ) : null}
      </div>
    );
  }

  const urgent = !expired && secondsLeft <= 10;
  const progress = expired ? 0 : Math.min(100, (secondsLeft / validForSeconds) * 100);

  return (
    <div className={cx(styles.panel, expired && styles.expired)}>
      <dl className={styles.figures}>
        <div>
          <dt>Exchange rate</dt>
          <dd>
            1 {quote.sellCurrency} = {formatRate(quote.rate)} {quote.buyCurrency}
          </dd>
        </div>
        <div>
          <dt>{sellLabel}</dt>
          <dd className={styles.amount}>{formatMoney(quote.sellAmount, quote.sellCurrency)}</dd>
        </div>
        <div>
          <dt>{buyLabel}</dt>
          <dd className={styles.amount}>{formatMoney(quote.buyAmount, quote.buyCurrency)}</dd>
        </div>
      </dl>

      {expired ? (
        <Alert
          tone="warning"
          title="This rate has expired"
          actions={
            <Button size="sm" onClick={onRefresh} loading={loading}>
              Get a new quote
            </Button>
          }
        >
          Rates are only held for {validForSeconds} seconds. The amounts may change when you get a new quote.
        </Alert>
      ) : (
        <div className={styles.countdown}>
          <span role="timer" aria-live="off" className={cx(styles.time, urgent && styles.urgent)}>
            Rate valid for {formatCountdown(secondsLeft)}
          </span>
          <div className={styles.track} aria-hidden="true">
            <div className={cx(styles.bar, urgent && styles.urgentBar)} style={{ width: `${progress}%` }} />
          </div>
        </div>
      )}

      {error && !expired ? <Alert tone="danger">{error}</Alert> : null}

      <VisuallyHidden aria-live="polite" role="status">
        {announcementFor(expired, secondsLeft, validForSeconds)}
      </VisuallyHidden>
    </div>
  );
}
