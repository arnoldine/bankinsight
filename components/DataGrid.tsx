import React, { useState } from 'react';
import { Account } from '../types';
import { Search, Filter, Download, Book, MoreHorizontal } from 'lucide-react';

interface DataGridProps {
  data: Account[];
}

const DataGrid: React.FC<DataGridProps> = ({ data }) => {
  const [searchTerm, setSearchTerm] = useState('');

  const filteredData = data.filter(item => 
    item.id.toLowerCase().includes(searchTerm.toLowerCase()) ||
    item.cif.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const handleExport = () => {
    if (filteredData.length === 0) {
      alert("No data to export.");
      return;
    }

    // Define CSV Headers
    const headers = ['Account No', 'Client (CIF)', 'Type', 'Product Code', 'Balance', 'Lien Amount', 'Status', 'Last Transaction'];
    
    // Map data to CSV rows
    const csvRows = filteredData.map(row => {
      return [
        `"${row.id}"`,
        `"${row.cif}"`,
        `"${row.type}"`,
        `"${row.productCode}"`,
        row.balance.toFixed(2),
        row.lienAmount.toFixed(2),
        `"${row.status}"`,
        `"${row.lastTransDate}"`
      ].join(',');
    });

    // Combine headers and rows
    const csvContent = [headers.join(','), ...csvRows].join('\n');

    // Create a Blob and trigger download
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `accounts_export_${new Date().toISOString().split('T')[0]}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  return (
    <div className="flex flex-col h-full">
      {/* Table Toolbar */}
      <div className="p-4 bg-gray-50 border-b border-gray-200 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div className="flex items-center gap-2">
           <div className="relative">
             <input 
               type="text" 
               placeholder="Filter by name or account..." 
               className="pl-3 pr-4 py-1.5 border border-gray-300 rounded text-sm w-64 focus:outline-none focus:border-blue-500"
               value={searchTerm}
               onChange={(e) => setSearchTerm(e.target.value)}
             />
           </div>
        </div>
        
        <div className="flex items-center gap-2">
          <button className="flex items-center gap-1 px-3 py-1.5 bg-white border border-gray-300 rounded text-sm text-gray-600 hover:bg-gray-50">
            <Filter size={14} /> Filter
          </button>
          <button 
            onClick={handleExport}
            className="flex items-center gap-1 px-3 py-1.5 bg-white border border-gray-300 rounded text-sm text-gray-600 hover:bg-gray-50"
          >
            <Download size={14} /> Export
          </button>
        </div>
      </div>
      
      {/* Table Area */}
      <div className="overflow-auto flex-1">
        <table className="w-full text-left text-sm border-collapse">
          <thead className="bg-gray-100 text-gray-600 font-semibold sticky top-0 border-b border-gray-200">
            <tr>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Account No</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Client (CIF)</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider">Product</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-right">Balance</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-right">Lien</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-center">Status</th>
              <th className="px-4 py-3 text-xs uppercase tracking-wider text-center">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {filteredData.slice(0, 50).map((row, idx) => (
              <tr key={row.id} className={`hover:bg-blue-50 transition-colors ${idx % 2 === 0 ? 'bg-white' : 'bg-gray-50'}`}>
                <td className="px-4 py-2.5 font-medium text-blue-600 cursor-pointer hover:underline">{row.id}</td>
                <td className="px-4 py-2.5 text-gray-700">{row.cif}</td>
                <td className="px-4 py-2.5 text-gray-600">
                   {row.type} <span className="text-xs text-gray-400 ml-1">({row.productCode})</span>
                </td>
                <td className="px-4 py-2.5 text-right font-medium text-gray-800">{row.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                <td className="px-4 py-2.5 text-right text-gray-500">{row.lienAmount > 0 ? row.lienAmount.toLocaleString() : '-'}</td>
                <td className="px-4 py-2.5 text-center">
                  <span className={`inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold uppercase
                    ${row.status === 'ACTIVE' ? 'bg-green-100 text-green-700' : 
                      row.status === 'DORMANT' ? 'bg-yellow-100 text-yellow-700' : 
                      'bg-red-100 text-red-700'}`}>
                    {row.status}
                  </span>
                </td>
                <td className="px-4 py-2.5 text-center">
                   <button className="text-gray-400 hover:text-blue-600">
                      <MoreHorizontal size={16} />
                   </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {filteredData.length === 0 && (
            <div className="p-8 text-center text-gray-500 bg-white">No records found.</div>
        )}
      </div>
      <div className="bg-gray-50 border-t border-gray-200 p-2 text-xs text-gray-500 text-right">
         Showing {filteredData.length} entries
      </div>
    </div>
  );
};

export default DataGrid;