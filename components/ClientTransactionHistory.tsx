
import React from 'react';
import { Transaction } from '../types';

interface ClientTransactionHistoryProps {
  transactions: Transaction[];
}

const ClientTransactionHistory: React.FC<ClientTransactionHistoryProps> = ({ transactions }) => {
  return (
    <div className="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
      <table className="w-full text-sm text-left">
        <thead className="bg-gray-50 text-gray-500 border-b border-gray-200 sticky top-0">
          <tr>
            <th className="p-4">Date</th>
            <th className="p-4">Type</th>
            <th className="p-4">Account</th>
            <th className="p-4">Narration</th>
            <th className="p-4 text-right">Amount</th>
            <th className="p-4 text-center">Status</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-gray-100">
          {transactions.map((tx) => (
            <tr key={tx.id} className="hover:bg-gray-50 transition-colors">
              <td className="p-4 text-gray-600 whitespace-nowrap">
                {new Date(tx.date).toLocaleString()}
              </td>
              <td className="p-4">
                <span
                  className={`px-2 py-0.5 rounded text-xs font-bold ${
                    tx.type === 'DEPOSIT'
                      ? 'bg-green-100 text-green-700'
                      : tx.type === 'WITHDRAWAL'
                      ? 'bg-red-100 text-red-700'
                      : 'bg-blue-100 text-blue-700'
                  }`}
                >
                  {tx.type}
                </span>
              </td>
              <td className="p-4 font-mono text-gray-800 text-xs">{tx.accountId}</td>
              <td className="p-4 text-gray-500 text-xs truncate max-w-xs" title={tx.narration}>
                {tx.narration}
              </td>
              <td className="p-4 text-right font-mono font-medium">
                {tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
              </td>
              <td className="p-4 text-center">
                <span
                  className={`text-xs font-bold uppercase ${
                    tx.status === 'POSTED' ? 'text-green-600' : 'text-yellow-600'
                  }`}
                >
                  {tx.status}
                </span>
              </td>
            </tr>
          ))}
          {transactions.length === 0 && (
            <tr>
              <td colSpan={6} className="p-12 text-center text-gray-400">
                No transaction history found.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
};

export default ClientTransactionHistory;
