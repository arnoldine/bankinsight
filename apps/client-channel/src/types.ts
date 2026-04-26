export type NavItemId =
  | "dashboard"
  | "accounts"
  | "statements"
  | "activity"
  | "alerts"
  | "messages"
  | "complaints"
  | "profile"
  | "security"
  | "support";

export interface NavItem {
  id: NavItemId;
  label: string;
  description: string;
  requiresStepUp?: boolean;
}

export interface SecurityMetric {
  label: string;
  value: string;
  tone: "stable" | "warning" | "critical";
}

export interface TimelineEvent {
  id: string;
  title: string;
  detail: string;
  timestamp: string;
  status: "open" | "review" | "resolved";
}

export interface ComplaintRecord {
  reference: string;
  category: string;
  status: "Acknowledged" | "Under Review" | "Awaiting Customer Input" | "Resolved";
  updatedAt: string;
}

export interface DeviceSession {
  id: string;
  name: string;
  location: string;
  lastSeen: string;
  trusted: boolean;
}
