import type { ComplaintItem, DeviceItem, MetricCard } from "./types";

export const metricCards: MetricCard[] = [
  { label: "MFA", value: "Protected", tone: "stable" },
  { label: "Session Risk", value: "Low", tone: "stable" },
  { label: "Open Complaints", value: "2", tone: "warning" },
  { label: "Active Devices", value: "3", tone: "warning" }
];

export const complaintItems: ComplaintItem[] = [
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

export const deviceItems: DeviceItem[] = [
  {
    id: "dev-001",
    name: "Pixel 8 Pro",
    location: "Accra, Ghana",
    lastSeen: "08 Apr 2026, 10:42 GMT",
    trusted: true
  },
  {
    id: "dev-002",
    name: "iPhone 15",
    location: "Tema, Ghana",
    lastSeen: "08 Apr 2026, 07:16 GMT",
    trusted: true
  },
  {
    id: "dev-003",
    name: "Chrome Web Session",
    location: "Unknown location",
    lastSeen: "07 Apr 2026, 22:11 GMT",
    trusted: false
  }
];
