import React from 'react';
import EnhancedDashboardLayout from './EnhancedDashboardLayout';
import { User } from '../services/authService';

interface DashboardLayoutProps {
  user: User;
  onLogout: () => void;
  error?: string | null;
  onErrorDismiss?: () => void;
}

// Legacy entry point retained for compatibility.
// The production shell now lives entirely in EnhancedDashboardLayout so
// older imports cannot fall back to a placeholder-heavy dashboard.
export default function DashboardLayout(props: DashboardLayoutProps) {
  return <EnhancedDashboardLayout {...props} />;
}
