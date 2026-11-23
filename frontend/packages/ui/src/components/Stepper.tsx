import { cx } from "./cx";
import styles from "./Stepper.module.css";

export type StepperProps = {
  steps: string[];
  current: number;
  label?: string;
};

export function Stepper({ steps, current, label = "Progress" }: StepperProps) {
  return (
    <nav aria-label={label}>
      <ol className={styles.list}>
        {steps.map((step, index) => {
          const state = index < current ? "done" : index === current ? "current" : "todo";
          return (
            <li key={step} className={cx(styles.item, styles[state])} aria-current={state === "current" ? "step" : undefined}>
              <span className={styles.marker} aria-hidden="true">
                {state === "done" ? "✓" : index + 1}
              </span>
              <span className={styles.text}>
                {step}
                {state === "done" ? <span className={styles.sr}> (completed)</span> : null}
              </span>
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
