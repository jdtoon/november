import { formatMoney } from "../format";
import { cx } from "./cx";
import styles from "./MoneyAmount.module.css";

export type MoneyAmountProps = {
  amount: number;
  currency: string;
  emphasis?: boolean;
  className?: string;
};

export function MoneyAmount({ amount, currency, emphasis = false, className }: MoneyAmountProps) {
  return <span className={cx(styles.money, emphasis && styles.emphasis, className)}>{formatMoney(amount, currency)}</span>;
}
