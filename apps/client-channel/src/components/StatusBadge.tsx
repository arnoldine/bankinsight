interface StatusBadgeProps {
  tone: "stable" | "warning" | "critical";
  children: React.ReactNode;
}

export function StatusBadge({ tone, children }: StatusBadgeProps) {
  return <span className={`badge badge-${tone}`}>{children}</span>;
}
