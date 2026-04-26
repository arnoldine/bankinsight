import { SectionCard } from "../components/SectionCard";
import { StatusBadge } from "../components/StatusBadge";

export function AlertsPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Alerts and notifications" description="Security alerts should be prominent, timestamped, and easy to action.">
        <div className="stack">
          <div className="notice">
            <div>
              <strong>New device seen</strong>
              <p>Edge on Windows attempted a session from an unrecognized location.</p>
            </div>
            <StatusBadge tone="warning">review now</StatusBadge>
          </div>
          <div className="notice">
            <div>
              <strong>Complaint update available</strong>
              <p>Complaint BI-2026-00018 moved to Under Review.</p>
            </div>
            <StatusBadge tone="stable">updated</StatusBadge>
          </div>
        </div>
      </SectionCard>
    </div>
  );
}
