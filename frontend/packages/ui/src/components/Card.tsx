import type { HTMLAttributes, ReactNode } from "react";
import { cx } from "./cx";
import styles from "./Card.module.css";

export type CardProps = Omit<HTMLAttributes<HTMLElement>, "title"> & {
  title?: ReactNode;
  actions?: ReactNode;
  padded?: boolean;
};

export function Card({ title, actions, padded = true, className, children, ...rest }: CardProps) {
  return (
    <section className={cx(styles.card, className)} {...rest}>
      {title || actions ? (
        <header className={styles.header}>
          {title ? <h2 className={styles.title}>{title}</h2> : <span />}
          {actions ? <div className={styles.actions}>{actions}</div> : null}
        </header>
      ) : null}
      <div className={padded ? styles.body : undefined}>{children}</div>
    </section>
  );
}
