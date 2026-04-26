import { SectionCard } from "../components/SectionCard";

export function StatementsPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Statements" description="Exports should be audited and can require step-up authentication when session risk is elevated.">
        <div className="table-like">
          <div className="row header">
            <span>Period</span>
            <span>Account</span>
            <span>Status</span>
          </div>
          <div className="row">
            <span>March 2026</span>
            <span>Primary Account •••• 1092</span>
            <span>Ready for secure download</span>
          </div>
          <div className="row">
            <span>February 2026</span>
            <span>Business Account •••• 2048</span>
            <span>Ready for secure download</span>
          </div>
        </div>
      </SectionCard>
    </div>
  );
}
