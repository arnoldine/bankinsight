import path from 'path';
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, '.', '');
    return {
      server: {
        port: 3000,
        host: '0.0.0.0',
      },
      plugins: [react()],
      define: {
        'process.env.API_KEY': JSON.stringify(env.GEMINI_API_KEY),
        'process.env.GEMINI_API_KEY': JSON.stringify(env.GEMINI_API_KEY)
      },
      resolve: {
        alias: {
          '@': path.resolve(__dirname, '.'),
        }
      },
      build: {
        rollupOptions: {
          output: {
            manualChunks(id) {
              if (id.includes('node_modules')) {
                if (id.includes('@clerk')) return 'vendor-clerk';
                if (id.includes('react') || id.includes('scheduler')) return 'vendor-react';
                if (id.includes('lucide-react')) return 'vendor-icons';
                return 'vendor';
              }

              if (id.includes('EnhancedDashboardLayout') || id.includes('AppIntegrated')) return 'app-shell';
              if (id.includes('LoanManagementHub') || id.includes('LoanOfficerWorkspace') || id.includes('ApprovalInbox') || id.includes('ReportingHub') || id.includes('AccountingEngine') || id.includes('AccountantWorkspace')) return 'workspace-banking';
              if (id.includes('TreasuryManagementHub') || id.includes('VaultManagementHub') || id.includes('FxTradingDesk') || id.includes('FxRateManagement') || id.includes('InvestmentPortfolio')) return 'workspace-treasury';
              if (id.includes('Settings') || id.includes('ProcessDesigner') || id.includes('SecurityOperationsHub') || id.includes('AuditTrail') || id.includes('EodConsole')) return 'workspace-ops';
              if (id.includes('ClientManager') || id.includes('TellerTerminal') || id.includes('CustomerServiceWorkspace') || id.includes('TransactionExplorer') || id.includes('StatementVerification') || id.includes('GroupManager')) return 'workspace-frontoffice';

              return undefined;
            },
          },
        },
      },
      test: {
        environment: 'jsdom',
        setupFiles: './src/test/setup.ts',
        globals: true
      }
    };
});
