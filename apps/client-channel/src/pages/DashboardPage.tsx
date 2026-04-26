import { securityMetrics } from "../data";
import { SectionCard } from "../components/SectionCard";
import { StatusBadge } from "../components/StatusBadge";

export function DashboardPage() {
  return (
    <div className="page-grid">
      <SectionCard
        title="Trusted customer access"
        description="Phase 1 focuses on account visibility, secure support, complaints, and customer-controlled security."
      >
        <div className="hero-panel">
          <div>
            <p className="eyebrow">Compliance posture</p>
            <h3>BoG-aligned, audit-ready foundation</h3>
            <p>
              This scaffold is structured for MFA, immutable audit events, complaint
              lifecycle tracking, and privacy-led customer servicing.
            </p>
          </div>
          <div className="hero-actions">
            <button type="button">Review complaint journey</button>
            <button type="button" className="secondary">
              Configure security controls
            </button>
          </div>
        </div>
      </SectionCard>

      <div className="metric-grid">
        {securityMetrics.map((metric) => (
          <SectionCard key={metric.label} title={metric.label}>
            <div className="metric-row">
              <strong>{metric.value}</strong>
              <StatusBadge tone={metric.tone}>{metric.tone}</StatusBadge>
            </div>
          </SectionCard>
        ))}
      </div>

      <SectionCard
        title="Phase 1 implementation priorities"
        description="The first delivery pass should wire these screens to real IAM, account-read, statements, and complaints services."
      >
        <ul className="list">
          <li>Step-up authentication for profile, security, and sensitive document actions</li>
          <li>Complaint intake with evidence preservation and SLA-driven timeline states</li>
          <li>Session and device visibility with revoke-all controls</li>
          <li>Secure inbox and auditable customer support interactions</li>
        </ul>
      </SectionCard>
    </div>
  );
}
