import { SectionCard } from "../components/SectionCard";

export function ProfilePage() {
  return (
    <div className="page-grid">
      <SectionCard title="Profile and KYC" description="High-risk profile changes should trigger step-up authentication and review workflows.">
        <ul className="list">
          <li>Contact details with masked recovery channels</li>
          <li>Preferred communication methods and consent references</li>
          <li>KYC document upload placeholder with compliance review status</li>
          <li>Change history summary for customer visibility</li>
        </ul>
      </SectionCard>
    </div>
  );
}
