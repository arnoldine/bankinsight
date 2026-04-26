import type { ComplaintRecord, DeviceSession, NavItem, SecurityMetric, TimelineEvent } from "./types";

export const navItems: NavItem[] = [
  { id: "dashboard", label: "Dashboard", description: "Overview and trust signals" },
  { id: "accounts", label: "Accounts", description: "Balances and linked accounts" },
  { id: "statements", label: "Statements", description: "Statement library and exports" },
  { id: "activity", label: "Activity", description: "Recent account and channel activity" },
  { id: "alerts", label: "Alerts", description: "Security and service alerts" },
  { id: "messages", label: "Messages", description: "Secure communication with the bank" },
  { id: "complaints", label: "Complaints", description: "Formal complaint intake and tracking" },
  { id: "profile", label: "Profile", description: "Customer contact and KYC profile", requiresStepUp: true },
  { id: "security", label: "Security", description: "Devices, MFA, and sessions", requiresStepUp: true },
  { id: "support", label: "Support", description: "Help options and recourse guidance" }
];

export const securityMetrics: SecurityMetric[] = [
  { label: "MFA", value: "Protected", tone: "stable" },
  { label: "Session Risk", value: "Low", tone: "stable" },
  { label: "Active Devices", value: "3", tone: "warning" },
  { label: "Complaint SLA", value: "On Track", tone: "stable" }
];

export const complaintTimeline: TimelineEvent[] = [
  {
    id: "cmp-1",
    title: "Complaint acknowledged",
    detail: "Reference BI-2026-00018 acknowledged by customer operations.",
    timestamp: "08 Apr 2026, 09:15 GMT",
    status: "open"
  },
  {
    id: "cmp-2",
    title: "Under review",
    detail: "Evidence package routed to service quality and compliance queue.",
    timestamp: "08 Apr 2026, 10:20 GMT",
    status: "review"
  },
  {
    id: "cmp-3",
    title: "Customer update pending",
    detail: "Next update due within the published complaint handling SLA.",
    timestamp: "08 Apr 2026, 11:00 GMT",
    status: "review"
  }
];

export const complaintRecords: ComplaintRecord[] = [
  {
    reference: "BI-2026-00018",
    category: "Unauthorized access concern",
    status: "Under Review",
    updatedAt: "08 Apr 2026"
  },
  {
    reference: "BI-2026-00011",
    category: "Statement discrepancy",
    status: "Awaiting Customer Input",
    updatedAt: "06 Apr 2026"
  }
];

export const deviceSessions: DeviceSession[] = [
  {
    id: "dev-001",
    name: "Chrome on Windows",
    location: "Accra, Ghana",
    lastSeen: "08 Apr 2026, 10:42 GMT",
    trusted: true
  },
  {
    id: "dev-002",
    name: "Mobile Safari on iPhone",
    location: "Tema, Ghana",
    lastSeen: "08 Apr 2026, 07:16 GMT",
    trusted: true
  },
  {
    id: "dev-003",
    name: "Edge on Windows",
    location: "Unknown location",
    lastSeen: "07 Apr 2026, 22:11 GMT",
    trusted: false
  }
];
