const env = (import.meta as any).env;
const configuredBaseUrl = env.VITE_API_URL as string | undefined;

const resolveBaseUrl = (): string => {
  if (!configuredBaseUrl) {
    return 'http://localhost:5176/api';
  }

  // Render static sites commonly receive the API web service's external URL,
  // which does not include the /api prefix used by this app's controllers.
  try {
    const parsed = new URL(configuredBaseUrl);
    if (!parsed.pathname || parsed.pathname === '/') {
      parsed.pathname = '/api';
      return parsed.toString().replace(/\/$/, '');
    }
  } catch {
    // Ignore parse failures and fall through to the existing logic.
  }

  // In Vite dev mode, a relative /api base usually points back to the dev server.
  // Redirect to the local API server to avoid 300x/api -> 404/500 loops.
  if (env.DEV && configuredBaseUrl.startsWith('/')) {
    return `http://localhost:5176${configuredBaseUrl}`;
  }

  return configuredBaseUrl;
};

export const API_CONFIG = {
  baseUrl: resolveBaseUrl(),
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
};

export const API_ENDPOINTS = {
  auth: {
    login: '/auth/login',
    logout: '/auth/logout',
    validate: '/auth/validate',
    refresh: '/auth/refresh',
    me: '/auth/me',
    verifyMfa: '/auth/mfa/verify',
  },

  clerk: {
    me: '/clerk/me',
    sync: '/clerk/sync',
    webhook: '/clerk/webhook',
  },

  users: {
    list: '/users',
    create: '/users',
    get: (id: string) => `/users/${id}`,
    update: (id: string) => `/users/${id}`,
    delete: (id: string) => `/users/${id}`,
    sessions: (id: string) => `/users/${id}/sessions`,
    roles: '/roles',
    permissions: '/permissions',
  },

  branches: {
    list: '/branch',
    create: '/branch',
    get: (id: string) => `/branch/${id}`,
    update: (id: string) => `/branch/${id}`,
    hierarchy: '/branch/hierarchy',
    performance: (id: string) => `/branch/${id}/performance`,
    vault: (id: string) => `/branch/${id}/vault`,
  },

  config: {
    get: '/config',
    update: '/config',
  },

  orass: {
    profile: '/orass/profile',
    readiness: '/orass/readiness',
    queue: '/orass/queue',
    history: '/orass/history',
    submit: (returnId: number) => `/orass/submit/${returnId}`,
    evidence: (returnId: number) => `/orass/evidence/${returnId}`,
    acknowledge: (returnId: number) => `/orass/acknowledge/${returnId}`,
    reconcile: '/orass/reconcile',
  },

  treasury: {
    positions: '/treasury/positions',
    positions_detail: (id: string) => `/treasury/positions/${id}`,
    fxrates: '/treasury/fx-rates',
    fxrates_update: '/treasury/fx-rates/manual',
    fxtrades: '/treasury/fx-trades',
    fxtrades_create: '/treasury/fx-trades',
    investments: '/treasury/investments',
    investments_create: '/treasury/investments',
    riskmetrics: '/treasury/risk-metrics',
  },

  deposits: {
    list: '/deposits',
    create: '/deposits',
    get: (id: string) => `/deposits/${id}`,
    renew: (id: string) => `/deposits/${id}/renew`,
    close: (id: string) => `/deposits/${id}/close`,
  },

  vault: {
    base: '/vault',
    all: '/vault',
    byBranch: (branchId: string) => `/vault/branch/${branchId}`,
    detail: (branchId: string, currency: string) => `/vault/${branchId}/${currency}`,
    count: '/vault/count',
    transaction: '/vault/transaction',
    tills: '/vault/tills',
    openTill: '/vault/tills/open',
    allocateTill: '/vault/tills/allocate',
    returnTill: '/vault/tills/return',
    closeTill: '/vault/tills/close',
  },

  cashControl: {
    vaultCashPosition: '/cash-control/vault-cash-position',
    branchCashSummary: '/cash-control/branch-cash-summary',
    reconciliation: '/cash-control/reconciliation',
    transitItems: '/cash-control/transit-items',
  },

  cashIncidents: {
    list: '/cash-incidents',
    create: '/cash-incidents',
    resolve: (incidentId: string) => `/cash-incidents/${incidentId}/resolve`,
  },
  enterpriseReports: {
    catalog: '/reports/catalog',
    catalogItem: (code: string) => `/reports/catalog/${code}`,
    execute: (code: string) => `/reports/execute/${code}`,
    export: (code: string, format: string) => `/reports/export/${code}/${format}`,
    history: '/reports/history',
    favorites: '/reports/favorites',
    favorite: (code: string) => `/reports/favorites/${code}`,
    presets: (code: string) => `/reports/presets/${code}`,
    presetItem: (presetId: string) => `/reports/presets/item/${presetId}`,
    crbDataQuality: '/reports/crb/data-quality',
  },

  reports: {
    catalog: '/report/catalog',
    definitions: '/report/definitions',
    definitions_create: '/report/definitions',
    generate: '/report/generate',
    history: (id: string) => `/report/history/${id}`,
    dailyPosition: '/report/regulatory/daily-position',
    monthlyReturn1: '/report/regulatory/monthly-return-1',
    monthlyReturn2: '/report/regulatory/monthly-return-2',
    monthlyReturn3: '/report/regulatory/monthly-return-3',
    prudentialReturn: '/report/regulatory/prudential-return',
    largeExposure: '/report/regulatory/large-exposure',
    regulatoryReturns: '/report/regulatory/returns',
    submitReturn: (id: string) => `/report/regulatory/submit-to-bog/${id}`,
    balanceSheet: '/report/financial/balance-sheet',
    incomeStatement: '/report/financial/income-statement',
    cashFlow: '/report/financial/cash-flow',
    trialBalance: '/report/financial/trial-balance',
    customerSegmentation: '/report/analytics/customer-segmentation',
    transactionTrends: '/report/analytics/transaction-trends',
    productAnalytics: '/report/analytics/product-analytics',
    channelAnalytics: '/report/analytics/channel-analytics',
    staffProductivity: '/report/analytics/staff-productivity',
  },

  accounts: {
    list: '/accounts',
    create: '/accounts',
    get: (id: string) => `/accounts/${id}`,
    update: (id: string) => `/accounts/${id}`,
    transactions: (id: string) => `/accounts/${id}/transactions`,
  },

  loans: {
    list: '/loans',
    create: '/loans',
    get: (id: string) => `/loans/${id}`,
    update: (id: string) => `/loans/${id}`,
    disburse: (id: string) => `/loans/disburse`,
    repay: (id: string) => `/loans/${id}/repay`,
    schedule: (id: string) => `/loans/${id}/schedule`,
    apply: '/loans/apply',
    appraise: '/loans/appraise',
    approve: '/loans/approve',
    repayUnified: '/loans/repay',
    repayReverse: '/loans/repay/reverse',
    restructure: '/loans/restructure',
    writeoff: '/loans/writeoff',
    recover: '/loans/recover',
    statement: (id: string) => `/loans/${id}/statement`,
    classify: (id: string) => `/loans/${id}/classify`,
    accrualProcess: '/loans/accruals/process',
    checkCredit: '/loans/check-credit',
    creditProviders: '/loans/credit-bureau/providers',
    generateSchedule: '/loans/generate-schedule',
    delinquencyDashboard: '/loans/dashboards/delinquency',
    profitabilityReport: '/loans/reports/profitability',
    balanceSheetReport: '/loans/reports/balance-sheet',
    glPostings: (id: string) => `/loans/${id}/gl-postings`,
    configureProduct: '/loans/products/configure',
    configureAccountingProfile: '/loans/accounting-profiles/configure',
  },

  customers: {
    list: '/customers',
    create: '/customers',
    get: (id: string) => `/customers/${id}`,
    profile: (id: string) => `/customers/${id}/profile`,
    update: (id: string) => `/customers/${id}`,
    addNote: (id: string) => `/customers/${id}/notes`,
    addDocument: (id: string) => `/customers/${id}/documents`,
    accounts: (id: string) => `/customers/${id}/accounts`,
    kyc: (id: string) => `/customers/${id}/kyc`,
  },

  approvals: {
    list: '/approvals',
    create: '/approvals',
    update: (id: string) => `/approvals/${id}`,
  },

  operations: {
    eodStatus: '/operations/eod/status',
    runEodStep: '/operations/eod/run-step',
  },

  audit: {
    list: '/audit',
  },

  security: {
    summary: '/security/summary',
    alerts: '/security/alerts',
    failedLogins: '/security/failed-logins',
    devices: '/security/devices',
    deviceActions: (id: string) => `/security/devices/${id}/actions`,
    scanOutdated: '/security/devices/scan-outdated',
    irregularTransactions: '/security/irregular-transactions',
  },

  workflowDefinitions: {
    list: '/WorkflowDefinition',
    get: (definitionId: string) => `/WorkflowDefinition/${definitionId}`,
    create: '/WorkflowDefinition',
    createVersion: (definitionId: string) => `/WorkflowDefinition/${definitionId}/versions`,
    addStep: (versionId: string) => `/WorkflowDefinition/versions/${versionId}/steps`,
    addTransition: (versionId: string) => `/WorkflowDefinition/versions/${versionId}/transitions`,
    validate: (versionId: string) => `/WorkflowDefinition/versions/${versionId}/validate`,
    publish: (versionId: string) => `/WorkflowDefinition/versions/${versionId}/publish`,
  },

  workflowRuntime: {
    start: (processCode?: string) => processCode
      ? `/WorkflowRuntime/start?processCode=${encodeURIComponent(processCode)}`
      : '/WorkflowRuntime/start',
  },

  products: {
    list: '/products',
    create: '/products',
    get: (id: string) => `/products/${id}`,
    update: (id: string) => `/products/${id}`,
  },


  groupLending: {
    groups: '/group-lending/groups',
    group: (id: string) => `/group-lending/groups/${id}`,
    activateGroup: (id: string) => `/group-lending/groups/${id}/activate`,
    suspendGroup: (id: string) => `/group-lending/groups/${id}/suspend`,
    groupMembers: (id: string) => `/group-lending/groups/${id}/members`,
    groupMember: (id: string, memberId: string) => `/group-lending/groups/${id}/members/${memberId}`,
    centers: '/group-lending/centers',
    applications: '/group-lending/applications',
    application: (id: string) => `/group-lending/applications/${id}`,
    submitApplication: (id: string) => `/group-lending/applications/${id}/submit`,
    reviewApplication: (id: string) => `/group-lending/applications/${id}/review`,
    approveApplication: (id: string) => `/group-lending/applications/${id}/approve`,
    rejectApplication: (id: string) => `/group-lending/applications/${id}/reject`,
    disburseApplication: (id: string) => `/group-lending/applications/${id}/disburse`,
    meetings: '/group-lending/meetings',
    meeting: (id: string) => `/group-lending/meetings/${id}`,
    meetingAttendance: (id: string) => `/group-lending/meetings/${id}/attendance`,
    closeMeeting: (id: string) => `/group-lending/meetings/${id}/close`,
    collectionBatches: '/group-lending/collections/batches',
    collectionBatch: (id: string) => `/group-lending/collections/batches/${id}`,
    postCollectionBatch: (id: string) => `/group-lending/collections/batches/${id}/post`,
    reverseCollectionBatch: (id: string) => `/group-lending/collections/batches/${id}/reverse`,
    repayLoan: (loanId: string) => `/group-lending/loans/${loanId}/repayments`,
    loanSchedule: (loanId: string) => `/group-lending/loans/${loanId}/schedule`,
    loanStatement: (loanId: string) => `/group-lending/loans/${loanId}/statement`,
    productGroupRules: (id: string) => `/group-lending/product-designer/loan-products/${id}/group-rules`,
    productEligibilityRules: (id: string) => `/group-lending/product-designer/loan-products/${id}/eligibility-rules`,
    reportsPar: '/group-lending/reports/par',
    reportsGroupPerformance: '/group-lending/reports/group-performance',
    reportsOfficerPerformance: '/group-lending/reports/officer-performance',
    reportsCycleAnalysis: '/group-lending/reports/cycle-analysis',
    reportsDelinquency: '/group-lending/reports/delinquency',
    reportsMeetingCollections: '/group-lending/reports/meeting-collections',
  },  gl: {
    accounts: '/gl/accounts',
    journalEntries: '/gl/journal-entries',
    seedRegulatory: '/gl/accounts/seed-regulatory',
  },

  transactions: {
    list: '/transactions',
    get: (id: string) => `/transactions/${id}`,
    approve: (id: string) => `/transactions/${id}/approve`,
    reject: (id: string) => `/transactions/${id}/reject`,
  },

  privilegeLeases: {
    listMine: '/privilege-leases/me',
    listByStaff: (staffId: string) => `/privilege-leases/${staffId}`,
    create: '/privilege-leases',
    revoke: (leaseId: string) => `/privilege-leases/${leaseId}/revoke`,
  },
};
















