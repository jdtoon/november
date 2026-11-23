import { humanize } from "../format";
import { cx } from "./cx";
import styles from "./StatusBadge.module.css";

type Tone = "success" | "warning" | "danger" | "info" | "neutral";

const tones: Record<string, Tone> = {
  PendingApproval: "warning",
  ActionRequired: "warning",
  Processing: "info",
  Completed: "success",
  Credited: "success",
  Rejected: "danger",
  Cancelled: "neutral",
};

export type StatusBadgeProps = {
  status: string;
  label?: string;
};

export function StatusBadge({ status, label }: StatusBadgeProps) {
  const tone = tones[status] ?? "neutral";
  return <span className={cx(styles.badge, styles[tone])}>{label ?? humanize(status)}</span>;
}

export type ChipProps = {
  children: string;
};

export function Chip({ children }: ChipProps) {
  return <span className={cx(styles.badge, styles.chip)}>{children}</span>;
}
