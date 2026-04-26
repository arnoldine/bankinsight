import { deviceSessions } from "../data";
import { SectionCard } from "../components/SectionCard";

export function SecurityPage() {
  return (
    <div className="page-grid">
      <SectionCard title="Security center" description="Customers should be able to review trusted devices, active sessions, and protective controls in plain language.">
        <div className="table-like">
          <div className="row header">
            <span>Device</span>
            <span>Location</span>
            <span>Last seen</span>
            <span>Trust</span>
          </div>
          {deviceSessions.map((device) => (
            <div className="row" key={device.id}>
              <span>{device.name}</span>
              <span>{device.location}</span>
              <span>{device.lastSeen}</span>
              <span>{device.trusted ? "Trusted" : "Review"}</span>
            </div>
          ))}
        </div>
      </SectionCard>
    </div>
  );
}
