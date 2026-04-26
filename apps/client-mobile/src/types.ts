export type TabId =
  | "home"
  | "accounts"
  | "statements"
  | "alerts"
  | "complaints"
  | "profile"
  | "security";

export interface MetricCard {
  label: string;
  value: string;
  tone: "stable" | "warning" | "critical";
}

export interface ComplaintItem {
  reference: string;
  category: string;
  status: "Acknowledged" | "Under Review" | "Awaiting Customer Input" | "Resolved";
  updatedAt: string;
}

export interface DeviceItem {
  id: string;
  name: string;
  location: string;
  lastSeen: string;
  trusted: boolean;
}
