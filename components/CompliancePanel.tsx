import React from 'react';
import { ComplianceMetric, GLAccount, Account, Loan } from '../types';
import { ShieldCheck, AlertTriangle, FileText, Download, PieChart, Activity } from 'lucide-react';

interface CompliancePanelProps {
  metrics: ComplianceMetric[];
  glAccounts: GLAccount[];
  accounts: Account[];
  loans: Loan[];
}

const CompliancePanel: React.FC<CompliancePanelProps> = ({ metrics, glAccounts, accounts, loans }) => {
  
  // Helper to format currency for XML
  const formatXMLVal = (num: number) => num.toFixed(2);

  // --- GENERATORS ---

  const generateBS2 = (): string => {
      // Income Statement
      const income = glAccounts.filter(g => g.category === 'INCOME').reduce((sum, g) => sum + g.balance, 0);
      const expense = glAccounts.filter(g => g.category === 'EXPENSE').reduce((sum, g) => sum + g.balance, 0);
      const netProfit = income - expense;

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_BS2>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>INCOME_STATEMENT</ReportName>
        <Period>${new Date().toISOString().slice(0, 7)}</Period>
        <SubmissionDate>${new Date().toISOString()}</SubmissionDate>
        <Currency>GHS</Currency>
    </Header>
    <Data>
        <LineItem code="1000" description="Interest Income">
            <Value>${formatXMLVal(income)}</Value>
        </LineItem>
        <LineItem code="2000" description="Interest Expense">
            <Value>${formatXMLVal(expense)}</Value>
        </LineItem>
        <LineItem code="3000" description="Net Operating Profit">
            <Value>${formatXMLVal(netProfit)}</Value>
        </LineItem>
        <Breakdown>
            ${glAccounts.filter(g => g.category === 'INCOME' || g.category === 'EXPENSE').map(g => `
            <GLAccount code="${g.code}">
                <Name>${g.name}</Name>
                <Balance>${formatXMLVal(g.balance)}</Balance>
            </GLAccount>`).join('')}
        </Breakdown>
    </Data>
    <Hash>${Math.random().toString(36).substring(7)}</Hash>
</BoG_Return_BS2>`;
  };

  const generateBS3 = (): string => {
      // Balance Sheet
      const assets = glAccounts.filter(g => g.category === 'ASSET').reduce((sum, g) => sum + g.balance, 0);
      const liabilities = glAccounts.filter(g => g.category === 'LIABILITY').reduce((sum, g) => sum + g.balance, 0);
      const equity = glAccounts.filter(g => g.category === 'EQUITY').reduce((sum, g) => sum + g.balance, 0);

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_BS3>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>BALANCE_SHEET</ReportName>
        <Period>${new Date().toISOString().slice(0, 7)}</Period>
    </Header>
    <Data>
        <Section name="ASSETS">
            <Total>${formatXMLVal(assets)}</Total>
            ${glAccounts.filter(g => g.category === 'ASSET').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Section name="LIABILITIES">
            <Total>${formatXMLVal(liabilities)}</Total>
            ${glAccounts.filter(g => g.category === 'LIABILITY').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Section name="EQUITY">
            <Total>${formatXMLVal(equity)}</Total>
            ${glAccounts.filter(g => g.category === 'EQUITY').map(g => `<Item code="${g.code}" val="${formatXMLVal(g.balance)}" />`).join('')}
        </Section>
        <Check>
            <Eq>${formatXMLVal(assets - (liabilities + equity))}</Eq>
        </Check>
    </Data>
</BoG_Return_BS3>`;
  };

  const generateLR1 = (): string => {
      // Liquidity Return
      // Liquid Assets: Cash + Bank Balances (Usually GLs 101xx and 102xx)
      const liquidAssets = glAccounts
          .filter(g => g.code.startsWith('101') || g.code.startsWith('102'))
          .reduce((sum, g) => sum + g.balance, 0);
      
      // Volatile Liabilities: Customer Deposits
      const totalDeposits = accounts
          .filter(a => a.type === 'SAVINGS' || a.type === 'CURRENT')
          .reduce((sum, a) => sum + a.balance, 0);

      const ratio = totalDeposits > 0 ? (liquidAssets / totalDeposits) * 100 : 0;

      return `<?xml version="1.0" encoding="UTF-8"?>
<BoG_Return_LR1>
    <Header>
        <BankCode>O4W</BankCode>
        <ReportName>WEEKLY_LIQUIDITY</ReportName>
        <SubmissionDate>${new Date().toISOString()}</SubmissionDate>
    </Header>
    <Data>
        <Metric code="LQ01" name="Total Liquid Assets">
            <Value>${formatXMLVal(liquidAssets)}</Value>
        </Metric>
        <Metric code="LQ02" name="Total Volatile Liabilities">
            <Value>${formatXMLVal(totalDeposits)}</Value>
        </Metric>
        <Metric code="LQ03" name="Liquidity Ratio">
            <Value>${formatXMLVal(ratio)}%</Value>
            <Threshold>> 35%</Threshold>
            <Status>${ratio > 35 ? 'PASS' : 'FAIL'}</Status>
        </Metric>
        <Detail>
            <DepositCount>${accounts.length}</DepositCount>
            <ActiveLoans>${loans.filter(l => l.status === 'ACTIVE').length}</ActiveLoans>
        </Detail>
    </Data>
</BoG_Return_LR1>`;
  };

  const handleGenerateReturn = (reportCode: string) => {
      let xmlContent = '';
      let filename = '';

      switch (reportCode) {
          case 'BS2':
              xmlContent = generateBS2();
              filename = `BoG_BS2_IncomeStmt_${Date.now()}.xml`;
              break;
          case 'BS3':
              xmlContent = generateBS3();
              filename = `BoG_BS3_BalanceSheet_${Date.now()}.xml`;
              break;
          case 'LR1':
              xmlContent = generateLR1();
              filename = `BoG_LR1_Liquidity_${Date.now()}.xml`;
              break;
          default:
              alert("Report template not implemented yet.");
              return;
      }

      const blob = new Blob([xmlContent], { type: 'application/xml' });
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
  };

  return (
    <div className="h-full flex flex-col gap-6">
       {/* Header */}
       <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-100">
          <div className="flex justify-between items-start">
             <div>
                <h2 className="text-xl font-bold text-gray-900 flex items-center gap-2">
                    <ShieldCheck className="text-green-600" />
                    BoG ORASS Compliance
                </h2>
                <p className="text-sm text-gray-500 mt-1">
                    Bank of Ghana Online Regulatory Analytics Surveillance Systems (ORASS)
                </p>
             </div>
             <div className="flex gap-2">
                 <button 
                    onClick={() => handleGenerateReturn('BS2')}
                    className="flex items-center gap-2 px-3 py-2 bg-indigo-50 text-indigo-700 text-sm font-medium rounded-lg hover:bg-indigo-100 transition-colors"
                >
                    <FileText size={16} /> Generate Monthly Return
                 </button>
             </div>
          </div>
       </div>

       {/* Metrics Grid */}
       <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {metrics.map((m, idx) => (
             <div key={idx} className="bg-white p-5 rounded-xl border border-gray-100 shadow-sm relative overflow-hidden">
                <div className={`absolute top-0 left-0 w-1 h-full ${
                    m.status === 'pass' ? 'bg-green-500' : m.status === 'warning' ? 'bg-yellow-500' : 'bg-red-500'
                }`}></div>
                <div className="flex justify-between items-start mb-2">
                    <span className="text-xs font-bold text-gray-400 uppercase tracking-wider">{m.code}</span>
                    {m.status === 'fail' && <AlertTriangle size={16} className="text-red-500" />}
                </div>
                <h3 className="text-gray-600 text-sm font-medium">{m.label}</h3>
                <div className="mt-2 flex items-baseline gap-2">
                    <span className="text-2xl font-bold text-gray-900">{m.value}</span>
                    <span className="text-xs text-gray-500">Target: {m.threshold}</span>
                </div>
             </div>
          ))}
       </div>

       {/* Reports Section */}
       <div className="flex-1 bg-white rounded-xl shadow-sm border border-gray-100 p-6">
          <h3 className="font-semibold text-gray-800 mb-4">Prudential Returns Submission (ORASS)</h3>
          <table className="w-full text-sm text-left">
             <thead className="bg-gray-50 text-gray-500 font-medium">
                <tr>
                   <th className="p-3">Report Code</th>
                   <th className="p-3">Description</th>
                   <th className="p-3">Frequency</th>
                   <th className="p-3">Due Date</th>
                   <th className="p-3">Status</th>
                   <th className="p-3">Action</th>
                </tr>
             </thead>
             <tbody className="divide-y divide-gray-100">
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">BS2</td>
                   <td className="p-3 font-medium flex items-center gap-2"><PieChart size={14} className="text-gray-400"/> Income Statement</td>
                   <td className="p-3">Monthly</td>
                   <td className="p-3 text-red-600 font-medium">Oct 14, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-yellow-100 text-yellow-700 rounded text-xs font-bold">Pending Review</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('BS2')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">BS3</td>
                   <td className="p-3 font-medium flex items-center gap-2"><Activity size={14} className="text-gray-400"/> Balance Sheet</td>
                   <td className="p-3">Monthly</td>
                   <td className="p-3">Oct 14, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-yellow-100 text-yellow-700 rounded text-xs font-bold">Pending Review</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('BS3')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
                <tr className="hover:bg-gray-50">
                   <td className="p-3 font-mono text-blue-600 font-bold">LR1</td>
                   <td className="p-3 font-medium flex items-center gap-2"><Activity size={14} className="text-gray-400"/> Weekly Liquidity Return</td>
                   <td className="p-3">Weekly</td>
                   <td className="p-3">Oct 06, 2023</td>
                   <td className="p-3"><span className="px-2 py-1 bg-green-100 text-green-700 rounded text-xs font-bold">Submitted</span></td>
                   <td className="p-3">
                       <button onClick={() => handleGenerateReturn('LR1')} className="p-2 hover:bg-indigo-50 rounded-full text-gray-400 hover:text-indigo-600 transition-colors" title="Download XML">
                           <Download size={16} />
                       </button>
                   </td>
                </tr>
             </tbody>
          </table>
       </div>
    </div>
  );
};

export default CompliancePanel;