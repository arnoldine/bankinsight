import React, { useEffect, useMemo, useState } from 'react';
import { BarChart3, Download, RefreshCw } from 'lucide-react';
import {
  BalanceSheetDTO,
  CashFlowStatementDTO,
  IncomeStatementDTO,
  TrialBalanceDTO,
  reportService,
} from '../src/services/reportService';

type ReportType = 'balance-sheet' | 'income-statement' | 'cash-flow' | 'trial-balance';

const statementLabels: Record<ReportType, string> = {
  'balance-sheet': 'Balance Sheet',
  'income-statement': 'Income Statement',
  'cash-flow': 'Cash Flow Statement',
  'trial-balance': 'Trial Balance',
};

const currency = (amount: number) =>
  new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(Number(amount || 0));

const periodStartFromDate = (date: string) => {
  const parsed = new Date(`${date}T00:00:00`);
  return new Date(parsed.getFullYear(), parsed.getMonth(), 1).toISOString().split('T')[0];
};

const toCsv = (rows: Array<Record<string, string | number | boolean>>) => {
  if (rows.length === 0) {
    return '';
  }

  const headers = Object.keys(rows[0]);
  const escape = (value: unknown) => `"${String(value ?? '').replace(/"/g, '""')}"`;
  return [
    headers.join(','),
    ...rows.map((row) => headers.map((header) => escape(row[header])).join(',')),
  ].join('\n');
};

export default function FinancialStatements() {
  const [reportType, setReportType] = useState<ReportType>('balance-sheet');
  const [asOfDate, setAsOfDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [balanceSheet, setBalanceSheet] = useState<BalanceSheetDTO | null>(null);
  const [incomeStatement, setIncomeStatement] = useState<IncomeStatementDTO | null>(null);
  const [cashFlow, setCashFlow] = useState<CashFlowStatementDTO | null>(null);
  const [trialBalance, setTrialBalance] = useState<TrialBalanceDTO | null>(null);

  const loadFinancialData = async () => {
    setLoading(true);
    setError(null);

    try {
      if (reportType === 'balance-sheet') {
        setBalanceSheet(await reportService.getBalanceSheet(asOfDate));
        return;
      }

      if (reportType === 'trial-balance') {
        setTrialBalance(await reportService.getTrialBalance(asOfDate));
        return;
      }

      const periodStart = periodStartFromDate(asOfDate);
      if (reportType === 'income-statement') {
        setIncomeStatement(await reportService.getIncomeStatement(periodStart, asOfDate));
        return;
      }

      setCashFlow(await reportService.getCashFlowStatement(periodStart, asOfDate));
    } catch (err) {
      setError((err as Error).message || 'Unable to load the selected statement right now.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadFinancialData();
  }, [reportType, asOfDate]);

  const exportRows = useMemo(() => {
    if (reportType === 'balance-sheet' && balanceSheet) {
      return [
        ...balanceSheet.assets.map((item) => ({ section: 'Assets', lineItem: item.lineItem, amount: item.amount, percentage: item.percentage ?? 0 })),
        ...balanceSheet.liabilities.map((item) => ({ section: 'Liabilities', lineItem: item.lineItem, amount: item.amount, percentage: item.percentage ?? 0 })),
        ...balanceSheet.equity.map((item) => ({ section: 'Equity', lineItem: item.lineItem, amount: item.amount, percentage: item.percentage ?? 0 })),
      ];
    }

    if (reportType === 'income-statement' && incomeStatement) {
      return [
        ...incomeStatement.revenueItems.map((item) => ({ section: 'Revenue', lineItem: item.lineItem, amount: item.amount })),
        ...incomeStatement.expenseItems.map((item) => ({ section: 'Expense', lineItem: item.lineItem, amount: item.amount })),
      ];
    }

    if (reportType === 'cash-flow' && cashFlow) {
      return [
        ...cashFlow.operatingActivities.map((item) => ({ section: 'Operating', activity: item.activity, category: item.category, amount: item.amount })),
        ...cashFlow.investingActivities.map((item) => ({ section: 'Investing', activity: item.activity, category: item.category, amount: item.amount })),
        ...cashFlow.financingActivities.map((item) => ({ section: 'Financing', activity: item.activity, category: item.category, amount: item.amount })),
      ];
    }

    if (reportType === 'trial-balance' && trialBalance) {
      return trialBalance.accounts.map((item) => ({
        accountNumber: item.accountNumber,
        accountName: item.accountName,
        balance: item.balance,
        debitBalance: item.debitBalance,
        creditBalance: item.creditBalance,
      }));
    }

    return [];
  }, [balanceSheet, cashFlow, incomeStatement, reportType, trialBalance]);

  const handleExport = () => {
    const csv = toCsv(exportRows);
    if (!csv) {
      return;
    }

    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `${reportType}-${asOfDate}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
  };

  const renderSection = (title: string, rows: Array<{ lineItem: string; amount: number; percentage?: number }>, tone: string, total: number) => (
    <div className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
      <div className="flex items-center justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
        <span className={`rounded-full px-3 py-1 text-xs font-semibold ${tone}`}>{rows.length} lines</span>
      </div>
      <div className="space-y-3">
        {rows.map((row) => (
          <div key={`${title}-${row.lineItem}`} className="flex items-center justify-between border-b border-gray-100 pb-3">
            <div className="flex-1">
              <p className="text-gray-700 font-medium">{row.lineItem}</p>
              {typeof row.percentage === 'number' && (
                <p className="text-xs text-gray-500 mt-1">{row.percentage.toFixed(1)}% of section total</p>
              )}
            </div>
            <p className="text-right font-semibold text-gray-900 ml-4">{currency(row.amount)}</p>
          </div>
        ))}
      </div>
      <div className="mt-4 pt-4 border-t-2 border-gray-300 flex justify-between items-center">
        <p className="text-lg font-bold text-gray-900">Section total</p>
        <p className="text-2xl font-bold text-slate-900">{currency(total)}</p>
      </div>
    </div>
  );

  const renderReport = () => {
    if (reportType === 'balance-sheet' && balanceSheet) {
      return (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="rounded-2xl border border-blue-100 bg-blue-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-blue-600">Total assets</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(balanceSheet.totalAssets)}</p>
            </div>
            <div className="rounded-2xl border border-rose-100 bg-rose-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-rose-600">Total liabilities</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(balanceSheet.totalLiabilities)}</p>
            </div>
            <div className="rounded-2xl border border-emerald-100 bg-emerald-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-emerald-600">Total equity</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(balanceSheet.totalEquity)}</p>
            </div>
          </div>
          {renderSection('Assets', balanceSheet.assets, 'bg-blue-100 text-blue-700', balanceSheet.totalAssets)}
          {renderSection('Liabilities', balanceSheet.liabilities, 'bg-rose-100 text-rose-700', balanceSheet.totalLiabilities)}
          {renderSection('Equity', balanceSheet.equity, 'bg-emerald-100 text-emerald-700', balanceSheet.totalEquity)}
        </div>
      );
    }

    if (reportType === 'income-statement' && incomeStatement) {
      return (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="rounded-2xl border border-emerald-100 bg-emerald-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-emerald-600">Revenue</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(incomeStatement.totalRevenue)}</p>
            </div>
            <div className="rounded-2xl border border-amber-100 bg-amber-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-amber-600">Expenses</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(incomeStatement.totalExpenses)}</p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-slate-600">Net profit</p>
              <p className="mt-2 text-2xl font-bold text-slate-900">{currency(incomeStatement.netProfit)}</p>
            </div>
          </div>
          {renderSection('Revenue', incomeStatement.revenueItems, 'bg-emerald-100 text-emerald-700', incomeStatement.totalRevenue)}
          {renderSection('Expenses', incomeStatement.expenseItems, 'bg-amber-100 text-amber-700', incomeStatement.totalExpenses)}
        </div>
      );
    }

    if (reportType === 'cash-flow' && cashFlow) {
      return (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="rounded-2xl border border-blue-100 bg-blue-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-blue-600">Operating</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(cashFlow.netOperatingCashFlow)}</p>
            </div>
            <div className="rounded-2xl border border-purple-100 bg-purple-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-purple-600">Investing</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(cashFlow.netInvestingCashFlow)}</p>
            </div>
            <div className="rounded-2xl border border-amber-100 bg-amber-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-amber-600">Financing</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(cashFlow.netFinancingCashFlow)}</p>
            </div>
            <div className="rounded-2xl border border-slate-200 bg-slate-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-slate-600">Net change</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(cashFlow.netChangeInCash)}</p>
            </div>
          </div>
          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
            {[{ title: 'Operating activities', rows: cashFlow.operatingActivities }, { title: 'Investing activities', rows: cashFlow.investingActivities }, { title: 'Financing activities', rows: cashFlow.financingActivities }].map((section) => (
              <div key={section.title} className="bg-white rounded-lg border border-gray-200 p-6 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">{section.title}</h3>
                <div className="space-y-3">
                  {section.rows.map((row) => (
                    <div key={`${section.title}-${row.activity}`} className="flex items-center justify-between border-b border-gray-100 pb-3">
                      <div>
                        <p className="text-gray-700 font-medium">{row.activity}</p>
                        <p className="text-xs text-gray-500 mt-1">{row.category}</p>
                      </div>
                      <p className="text-right font-semibold text-gray-900">{currency(row.amount)}</p>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </div>
      );
    }

    if (reportType === 'trial-balance' && trialBalance) {
      return (
        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="rounded-2xl border border-blue-100 bg-blue-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-blue-600">Total debits</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(trialBalance.totalDebits)}</p>
            </div>
            <div className="rounded-2xl border border-rose-100 bg-rose-50 p-4">
              <p className="text-xs uppercase tracking-[0.16em] text-rose-600">Total credits</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{currency(trialBalance.totalCredits)}</p>
            </div>
            <div className={`rounded-2xl border p-4 ${trialBalance.isBalanced ? 'border-emerald-100 bg-emerald-50' : 'border-amber-100 bg-amber-50'}`}>
              <p className={`text-xs uppercase tracking-[0.16em] ${trialBalance.isBalanced ? 'text-emerald-600' : 'text-amber-600'}`}>Balance status</p>
              <p className="mt-2 text-xl font-bold text-slate-900">{trialBalance.isBalanced ? 'Balanced' : 'Out of balance'}</p>
            </div>
          </div>
          <div className="bg-white rounded-lg border border-gray-200 shadow-sm overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="p-3 text-left">Account</th>
                  <th className="p-3 text-right">Debit</th>
                  <th className="p-3 text-right">Credit</th>
                  <th className="p-3 text-right">Net Balance</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {trialBalance.accounts.map((row) => (
                  <tr key={row.accountNumber}>
                    <td className="p-3">
                      <p className="font-medium text-slate-900">{row.accountNumber}</p>
                      <p className="text-xs text-slate-500">{row.accountName}</p>
                    </td>
                    <td className="p-3 text-right font-mono">{currency(row.debitBalance)}</td>
                    <td className="p-3 text-right font-mono">{currency(row.creditBalance)}</td>
                    <td className="p-3 text-right font-mono">{currency(row.balance)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      );
    }

    return null;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <BarChart3 className="w-6 h-6 text-blue-600" />
            Financial Statements
          </h2>
          <p className="text-gray-600 mt-1">Production-ready financial reporting backed by the live general ledger and operations data.</p>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={loadFinancialData} className="rounded-lg border border-gray-200 px-4 py-2 text-sm font-medium text-gray-700 hover:bg-gray-50 flex items-center gap-2">
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>
          <button onClick={handleExport} disabled={exportRows.length === 0} className="bg-blue-600 hover:bg-blue-700 disabled:bg-blue-300 text-white px-4 py-2 rounded-lg flex items-center gap-2">
            <Download className="w-4 h-4" />
            Export CSV
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <div className="rounded-2xl border border-slate-200 bg-white p-4">
          <p className="text-xs uppercase tracking-[0.16em] text-slate-500">Statement</p>
          <p className="mt-2 text-lg font-semibold text-slate-900">{statementLabels[reportType]}</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4">
          <p className="text-xs uppercase tracking-[0.16em] text-slate-500">As of date</p>
          <p className="mt-2 text-lg font-semibold text-slate-900">{asOfDate}</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4">
          <p className="text-xs uppercase tracking-[0.16em] text-slate-500">Data source</p>
          <p className="mt-2 text-lg font-semibold text-slate-900">Live reporting API</p>
        </div>
        <div className="rounded-2xl border border-slate-200 bg-white p-4">
          <p className="text-xs uppercase tracking-[0.16em] text-slate-500">Operational use</p>
          <p className="mt-2 text-lg font-semibold text-slate-900">Finance review and audit pack</p>
        </div>
      </div>

      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-col md:flex-row gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">Report Type</label>
            <select value={reportType} onChange={(e) => setReportType(e.target.value as ReportType)} className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="balance-sheet">Balance Sheet</option>
              <option value="income-statement">Income Statement</option>
              <option value="cash-flow">Cash Flow Statement</option>
              <option value="trial-balance">Trial Balance</option>
            </select>
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">As of Date</label>
            <input type="date" value={asOfDate} onChange={(e) => setAsOfDate(e.target.value)} className="px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div className="text-sm text-gray-500">
            {reportType === 'income-statement' || reportType === 'cash-flow'
              ? `Period start: ${periodStartFromDate(asOfDate)}`
              : 'Single-date reporting view'}
          </div>
        </div>
      </div>

      {error && (
        <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
          {error}
        </div>
      )}

      {loading ? (
        <div className="text-center py-16">
          <div className="animate-spin inline-block w-8 h-8 border-4 border-blue-200 border-t-blue-600 rounded-full"></div>
          <p className="mt-4 text-gray-600">Loading {statementLabels[reportType].toLowerCase()}...</p>
        </div>
      ) : (
        renderReport()
      )}
    </div>
  );
}
