import { formatDateTime } from "../format";
import styles from "./Timeline.module.css";

export type TimelineItem = {
  at: string;
  event: string;
  by?: string;
  note?: string;
};

export type TimelineProps = {
  events: TimelineItem[];
};

export function Timeline({ events }: TimelineProps) {
  return (
    <ol className={styles.list}>
      {events.map((event, index) => (
        <li key={`${event.at}-${index}`} className={styles.item}>
          <span className={styles.dot} aria-hidden="true" />
          <div>
            <p className={styles.event}>
              {event.event}
              {event.by ? <span className={styles.by}> by {event.by}</span> : null}
            </p>
            <p className={styles.at}>
              <time dateTime={event.at}>{formatDateTime(event.at)}</time>
            </p>
            {event.note ? <p className={styles.note}>{event.note}</p> : null}
          </div>
        </li>
      ))}
    </ol>
  );
}
