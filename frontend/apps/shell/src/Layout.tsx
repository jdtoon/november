import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { NavLink, Outlet, useNavigate } from "react-router";
import { Button, Chip, VisuallyHidden } from "@november/ui";
import { api, queries, queryKeys } from "@november/shared";
import { useSession } from "./session";
import styles from "./Layout.module.css";

type NavItem = { to: string; label: string; end?: boolean; count?: number; countLabel?: string };

export function Layout() {
  const session = useSession();
  const client = useQueryClient();
  const navigate = useNavigate();
  const dashboard = useQuery({ ...queries.dashboard(), refetchInterval: 30_000 });

  const logout = useMutation({
    mutationFn: api.logout,
    onSettled: () => {
      client.removeQueries({ predicate: (query) => query.queryKey[0] !== "session" });
      client.setQueryData(queryKeys.session, null);
      navigate("/login", { replace: true });
    },
  });

  const items: NavItem[] = [
    { to: "/", label: "Dashboard", end: true },
    { to: "/payments", label: "Payments" },
    {
      to: "/approvals",
      label: "Approvals",
      count: dashboard.data?.awaitingMyApprovalCount,
      countLabel: "payments awaiting your approval",
    },
    {
      to: "/incoming",
      label: "Incoming",
      count: dashboard.data?.incomingActionRequiredCount,
      countLabel: "incoming payments need your action",
    },
  ];

  return (
    <div className={styles.shell}>
      <a className={styles.skip} href="#main">
        Skip to content
      </a>
      <header className={styles.header}>
        <div className={styles.headerInner}>
          <div className={styles.brand}>
            <span className={styles.logo}>November</span>
            <span className={styles.company}>{session.company.name}</span>
          </div>
          <div className={styles.user}>
            <span className={styles.name}>{session.displayName}</span>
            <span className={styles.roles}>
              {session.roles.map((role) => (
                <Chip key={role}>{role}</Chip>
              ))}
            </span>
            <Button variant="secondary" size="sm" onClick={() => logout.mutate()} loading={logout.isPending}>
              Sign out
            </Button>
          </div>
        </div>
        <nav className={styles.nav} aria-label="Main">
          <ul className={styles.navList}>
            {items.map((item) => (
              <li key={item.to}>
                <NavLink to={item.to} end={item.end} className={({ isActive }) => `${styles.link} ${isActive ? styles.active : ""}`}>
                  {item.label}
                  {item.count ? (
                    <span className={styles.badge}>
                      {item.count}
                      <VisuallyHidden> {item.countLabel}</VisuallyHidden>
                    </span>
                  ) : null}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
      </header>
      <main id="main" className={styles.main}>
        <Outlet />
      </main>
    </div>
  );
}
