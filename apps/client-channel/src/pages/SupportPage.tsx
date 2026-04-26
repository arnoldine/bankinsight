import { SectionCard } from "../components/SectionCard";

export function SupportPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Support and recourse" description="Support should be easy to reach, but formal complaints must remain clearly distinct and trackable.">
        <ul className="list">
          <li>Secure message support for account and service questions</li>
          <li>Formal complaint path with reference number and SLA disclosure</li>
          <li>Emergency fraud reporting guidance for suspected compromise</li>
          <li>Accessibility and assisted-service contact options</li>
        </ul>
      </SectionCard>
    </div>
  );
}
