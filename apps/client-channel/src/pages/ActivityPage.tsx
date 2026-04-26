import { SectionCard } from "../components/SectionCard";

export function ActivityPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Recent activity" description="Customer-visible channel activity helps detect misuse and reinforces trust.">
        <ul className="timeline">
          <li>08 Apr 2026, 10:42 GMT: Successful login from Chrome on Windows</li>
          <li>08 Apr 2026, 09:15 GMT: Complaint BI-2026-00018 submitted</li>
          <li>07 Apr 2026, 18:26 GMT: Statement for March 2026 viewed</li>
        </ul>
      </SectionCard>
    </div>
  );
}
