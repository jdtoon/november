import { cx } from "./cx";
import styles from "./FilterTabs.module.css";

export type FilterOption = {
  value: string;
  label: string;
};

export type FilterTabsProps = {
  label: string;
  options: FilterOption[];
  value: string;
  onChange: (value: string) => void;
};

export function FilterTabs({ label, options, value, onChange }: FilterTabsProps) {
  return (
    <div role="group" aria-label={label} className={styles.group}>
      {options.map((option) => (
        <button
          key={option.value}
          type="button"
          aria-pressed={option.value === value}
          className={cx(styles.tab, option.value === value && styles.active)}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}
