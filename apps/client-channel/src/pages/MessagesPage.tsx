import { SectionCard } from "../components/SectionCard";

export function MessagesPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Secure messages" description="Customer support conversations should stay inside the authenticated channel with a clear audit trail.">
        <div className="stack">
          <div className="notice">
            <strong>Service Quality Team</strong>
            <p>We have received your complaint and will share the next update within the published SLA.</p>
          </div>
          <div className="notice">
            <strong>Fraud Operations</strong>
            <p>Your recent login alert was reviewed. Please verify active devices in the Security section.</p>
          </div>
        </div>
      </SectionCard>
    </div>
  );
}
