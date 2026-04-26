import { useMemo, useState } from "react";
import { AppShell } from "./components/AppShell";
import { SectionCard } from "./components/SectionCard";
import { appConfig } from "./config";
import { navItems } from "./data";
import { AccountsPage } from "./pages/AccountsPage";
import { ActivityPage } from "./pages/ActivityPage";
import { AlertsPage } from "./pages/AlertsPage";
import { ComplaintsPage } from "./pages/ComplaintsPage";
import { DashboardPage } from "./pages/DashboardPage";
import { MessagesPage } from "./pages/MessagesPage";
import { ProfilePage } from "./pages/ProfilePage";
import { SecurityPage } from "./pages/SecurityPage";
import { StatementsPage } from "./pages/StatementsPage";
import { SupportPage } from "./pages/SupportPage";
import type { NavItemId } from "./types";

function CurrentPage({ view }: { view: NavItemId }) {
  switch (view) {
    case "dashboard":
      return <DashboardPage />;
    case "accounts":
      return <AccountsPage />;
    case "statements":
      return <StatementsPage />;
    case "activity":
      return <ActivityPage />;
    case "alerts":
      return <AlertsPage />;
    case "messages":
      return <MessagesPage />;
    case "complaints":
      return <ComplaintsPage />;
    case "profile":
      return <ProfilePage />;
    case "security":
      return <SecurityPage />;
    case "support":
      return <SupportPage />;
    default:
      return <DashboardPage />;
  }
}

export function App() {
  const [activeView, setActiveView] = useState<NavItemId>("dashboard");

  const activeLabel = useMemo(
    () => navItems.find((item) => item.id === activeView)?.label ?? "Dashboard",
    [activeView]
  );

  return (
    <AppShell activeView={activeView} items={navItems} onSelect={setActiveView}>
      <div className="content-header">
        <div>
          <p className="eyebrow">Scaffold environment</p>
          <h2>{activeLabel}</h2>
          <p className="muted">
            {appConfig.compliancePosture} client experience with Phase 1 read, support,
            complaint, and security modules.
          </p>
        </div>
        <div className="header-panel">
          <span>Privacy notice {appConfig.privacyNoticeVersion}</span>
          <span>Session timeout {appConfig.sessionTimeoutMinutes} min</span>
        </div>
      </div>

      <CurrentPage view={activeView} />

      <SectionCard
        title="Scaffold notes"
        description="Use this starter to wire real services, authorization policies, and audit emitters without changing the production admin surfaces."
      >
        <ul className="list">
          <li>Replace placeholder state with authenticated API queries via the BFF or gateway</li>
          <li>Enforce step-up challenges before profile, security, and document actions</li>
          <li>Emit append-only audit records for every customer and staff-visible event</li>
        </ul>
      </SectionCard>
    </AppShell>
  );
}
