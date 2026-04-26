import { SectionCard } from "../components/SectionCard";

export function AccountsPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Account overview" description="Read-only account visibility with masked identifiers and customer-safe summaries.">
        <div className="table-like">
          <div className="row header">
            <span>Account</span>
            <span>Type</span>
            <span>Available balance</span>
          </div>
          <div className="row">
            <span>Primary Account •••• 1092</span>
            <span>Savings</span>
            <span>GHS 78,402.14</span>
          </div>
          <div className="row">
            <span>Business Account •••• 2048</span>
            <span>Current</span>
            <span>GHS 251,900.30</span>
          </div>
        </div>
      </SectionCard>
    </div>
  );
}
