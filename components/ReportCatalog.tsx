import React, { useState, useEffect } from 'react';
import { FileText, BarChart3, TrendingUp, Users, Zap, Calendar } from 'lucide-react';

interface Report {
  id: number;
  reportCode: string;
  reportName: string;
  description: string;
  reportType: string;
  frequency: string;
  isActive: boolean;
}

export default function ReportCatalog() {
  const [reports, setReports] = useState<Report[]>([]);
  const [reportType, setReportType] = useState<string>('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchReports();
  }, [reportType]);

  const fetchReports = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('token');
      const url = reportType 
        ? `/api/reporting/definitions?reportType=${reportType}`
        : '/api/reporting/definitions';
      
      const response = await fetch(url, {
        headers: { Authorization: `Bearer ${token}` }
      });

      if (response.ok) {
        const data = await response.json();
        setReports(data);
      }
    } catch (error) {
      console.error('Error fetching reports:', error);
    }
    setLoading(false);
  };

  const reportTypeIcons: Record<string, React.ReactNode> = {
    'Regulatory': <Zap className="w-5 h-5" />,
    'Financial': <BarChart3 className="w-5 h-5" />,
    'Analytics': <TrendingUp className="w-5 h-5" />,
    'Operational': <Users className="w-5 h-5" />
  };

  const reportTypeColors: Record<string, string> = {
    'Regulatory': 'bg-red-50 border-red-200',
    'Financial': 'bg-blue-50 border-blue-200',
    'Analytics': 'bg-purple-50 border-purple-200',
    'Operational': 'bg-green-50 border-green-200'
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <FileText className="w-6 h-6 text-blue-600" />
            Report Catalog
          </h2>
          <p className="text-gray-600 mt-1">Browse and generate available reports</p>
        </div>
      </div>

      {/* Filter */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <label className="block text-sm font-medium text-gray-700 mb-2">Filter by Type</label>
        <div className="flex gap-2">
          {['', 'Regulatory', 'Financial', 'Analytics', 'Operational'].map(type => (
            <button
              key={type || 'all'}
              onClick={() => setReportType(type)}
              className={`px-3 py-1 rounded-full text-sm font-medium transition-colors ${
                reportType === type
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-100 text-gray-700 hover:bg-gray-200'
              }`}
            >
              {type || 'All Reports'}
            </button>
          ))}
        </div>
      </div>

      {/* Reports Grid */}
      {loading ? (
        <div className="text-center py-12">
          <div className="animate-spin inline-block w-8 h-8 border-4 border-blue-200 border-t-blue-600 rounded-full"></div>
          <p className="mt-4 text-gray-600">Loading reports...</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {reports.map(report => (
            <div key={report.id} className={`rounded-lg border-2 p-4 ${reportTypeColors[report.reportType] || 'bg-gray-50 border-gray-200'}`}>
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center gap-2">
                  <div className="p-2 bg-white rounded-lg">
                    {reportTypeIcons[report.reportType] || <FileText className="w-5 h-5" />}
                  </div>
                  <div>
                    <h3 className="font-semibold text-gray-900">{report.reportName}</h3>
                    <p className="text-xs text-gray-600">{report.reportCode}</p>
                  </div>
                </div>
                <span className={`text-xs font-medium px-2 py-1 rounded-full ${
                  report.isActive 
                    ? 'bg-green-100 text-green-800'
                    : 'bg-gray-100 text-gray-600'
                }`}>
                  {report.isActive ? 'Active' : 'Inactive'}
                </span>
              </div>

              <p className="text-sm text-gray-700 mb-3">{report.description}</p>

              <div className="flex items-center justify-between text-xs text-gray-600 mb-4">
                <span className="flex items-center gap-1">
                  <BarChart3 className="w-4 h-4" />
                  {report.reportType}
                </span>
                <span className="flex items-center gap-1">
                  <Calendar className="w-4 h-4" />
                  {report.frequency}
                </span>
              </div>

              <button className="w-full bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 rounded-lg transition-colors">
                Generate Report
              </button>
            </div>
          ))}
        </div>
      )}

      {!loading && reports.length === 0 && (
        <div className="text-center py-12 bg-white rounded-lg border border-gray-200">
          <FileText className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-600">No reports available in this category</p>
        </div>
      )}
    </div>
  );
}
