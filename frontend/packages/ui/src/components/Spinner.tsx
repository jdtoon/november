import { cx } from "./cx";
import styles from "./Spinner.module.css";

export type SpinnerProps = {
  size?: "md" | "sm";
  label?: string;
};

export function Spinner({ size = "md", label = "Loading" }: SpinnerProps) {
  return (
    <span className={styles.wrap} role={label ? "status" : undefined}>
      <span className={cx(styles.spinner, size === "sm" && styles.sm)} aria-hidden="true" />
      {label ? <span className={styles.label}>{label}</span> : null}
    </span>
  );
}
