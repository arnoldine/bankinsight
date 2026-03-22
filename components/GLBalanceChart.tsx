
import React, { useMemo } from 'react';
import { GLAccount } from '../types';
import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, Tooltip, CartesianGrid, Cell } from 'recharts';

interface GLBalanceChartProps {
  accounts: GLAccount[];
}

const GLBalanceChart: React.FC<GLBalanceChartProps> = ({ accounts }) => {
  const data = useMemo(() => {
    // Filter non-header accounts, sort by absolute balance descending, take top 5
    return accounts
      .filter(acc => !acc.isHeader)
      .sort((a, b) => Math.abs(b.balance) - Math.abs(a.balance))
      .slice(0, 5)
      .map(acc => ({
        name: acc.name,
        code: acc.code,
        balance: acc.balance,
        absBalance: Math.abs(acc.balance)
      }));
  }, [accounts]);

  return (
    <div className="bg-white p-6 rounded-xl shadow-sm border border-gray-200 h-full flex flex-col">
      <div className="mb-6 flex justify-between items-end">
          <div>
            <h3 className="font-bold text-gray-800">Top GL Exposures</h3>
            <p className="text-xs text-gray-500 mt-1">Top 5 accounts by absolute balance magnitude</p>
          </div>
          <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded font-mono">
              REAL-TIME
          </span>
      </div>
      <div className="flex-1 w-full min-h-[250px]">
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} layout="vertical" margin={{ top: 5, right: 30, left: 40, bottom: 5 }}>
            <CartesianGrid strokeDasharray="3 3" horizontal={false} vertical={true} stroke="#f0f0f0" />
            <XAxis type="number" hide />
            <YAxis 
                dataKey="name" 
                type="category" 
                width={140} 
                tick={{fontSize: 11, fill: '#6b7280'}} 
                axisLine={false}
                tickLine={false}
            />
            <Tooltip 
                cursor={{fill: '#f9fafb'}}
                formatter={(value: number) => [`GHS ${value.toLocaleString()}`, 'Balance']}
                contentStyle={{ borderRadius: '8px', border: '1px solid #e5e7eb', boxShadow: '0 4px 6px -1px rgb(0 0 0 / 0.1)' }}
            />
            <Bar dataKey="balance" radius={[0, 4, 4, 0]} barSize={24}>
                {data.map((entry, index) => (
                    <Cell key={`cell-${index}`} fill={entry.balance >= 0 ? '#10b981' : '#ef4444'} />
                ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};

export default GLBalanceChart;
