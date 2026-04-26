import type { NavItem, NavItemId } from "../types";

interface AppShellProps {
  activeView: NavItemId;
  items: NavItem[];
  onSelect: (item: NavItemId) => void;
  children: React.ReactNode;
}

export function AppShell({ activeView, items, onSelect, children }: AppShellProps) {
  return (
    <div className="app-shell">
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="brand-block">
          <p className="eyebrow">BankInsight</p>
          <h1>Client Channel</h1>
          <p className="muted">
            Secure self-service for customers, complaints, and account visibility.
          </p>
        </div>

        <nav className="nav-list">
          {items.map((item) => (
            <button
              key={item.id}
              className={item.id === activeView ? "nav-item is-active" : "nav-item"}
              onClick={() => onSelect(item.id)}
              type="button"
            >
              <span>{item.label}</span>
              <small>{item.requiresStepUp ? "Step-up protected" : item.description}</small>
            </button>
          ))}
        </nav>
      </aside>

      <main className="content">{children}</main>
    </div>
  );
}
