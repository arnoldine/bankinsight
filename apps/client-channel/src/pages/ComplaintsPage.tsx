import { complaintRecords, complaintTimeline } from "../data";
import { SectionCard } from "../components/SectionCard";

export function ComplaintsPage() {
  return (
    <div className="page-grid page-grid-two">
      <SectionCard
        title="Complaint register"
        description="Formal complaints need acknowledgement, SLA visibility, and immutable event history."
      >
        <div className="table-like">
          <div className="row header">
            <span>Reference</span>
            <span>Category</span>
            <span>Status</span>
            <span>Updated</span>
          </div>
          {complaintRecords.map((record) => (
            <div className="row" key={record.reference}>
              <span>{record.reference}</span>
              <span>{record.category}</span>
              <span>{record.status}</span>
              <span>{record.updatedAt}</span>
            </div>
          ))}
        </div>
      </SectionCard>

      <SectionCard title="Complaint timeline" description="This panel models the regulator-friendly evidence trail customers and reviewers should see.">
        <ol className="timeline">
          {complaintTimeline.map((event) => (
            <li key={event.id}>
              <strong>{event.title}</strong>
              <p>{event.detail}</p>
              <small>{event.timestamp}</small>
            </li>
          ))}
        </ol>
      </SectionCard>
    </div>
  );
}
