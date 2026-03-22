
export interface SuspiciousTransactionReport {
    id: string;
    timestamp: string;
    accountId: string;
    amount: number;
    type: string;
    tellerId: string;
    reason: string;
    riskLevel: 'HIGH' | 'CRITICAL';
}

const REPORT_LOG: SuspiciousTransactionReport[] = [];

/**
 * Service to handle Anti-Money Laundering (AML) Reporting.
 * Logs suspicious transactions that exceed regulatory thresholds.
 */
export const reportSuspiciousActivity = (
    accountId: string,
    amount: number,
    type: string,
    tellerId: string,
    reason: string
) => {
    const report: SuspiciousTransactionReport = {
        id: `STR-${Date.now()}-${Math.floor(Math.random() * 1000)}`,
        timestamp: new Date().toISOString(),
        accountId,
        amount,
        type,
        tellerId,
        reason,
        riskLevel: amount > 50000 ? 'CRITICAL' : 'HIGH'
    };
    
    REPORT_LOG.push(report);
    
    // Simulate external system integration (e.g., GoAML or FIC Hub)
    console.group('%c🚨 AML SUSPICIOUS TRANSACTION ALERT 🚨', 'color: red; font-weight: bold; font-size: 16px; background: #ffebee; padding: 4px; border-radius: 4px;');
    console.log(`Transaction ID: ${report.id}`);
    console.log(`Account: ${accountId}`);
    console.log(`Amount: ${amount.toLocaleString()} (${type})`);
    console.log(`Reason: ${reason}`);
    console.log(`Timestamp: ${report.timestamp}`);
    console.groupEnd();
    
    return report;
};

export const getSuspiciousReports = () => [...REPORT_LOG];
