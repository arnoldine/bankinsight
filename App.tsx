import React from 'react';
import AppIntegrated from './src/AppIntegrated';

/**
 * DEPRECATED: Standard monolithic App template with standalone sidebar.
 * This has been replaced by the modular AppIntegrated and EnhancedDashboardLayout.
 * 
 * Delegating execution to AppIntegrated directly.
 */
export default function App() {
  return <AppIntegrated />;
}