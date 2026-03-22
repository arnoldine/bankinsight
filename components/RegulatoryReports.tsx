import React, { useState, useEffect } from 'react';
import { FileText, AlertCircle, CheckCircle, Clock, Download, Send } from 'lucide-react';

interface RegulatoryReturn {
  id: number;
  returnType: string;
  returnDate: string;
  submissionStatus: string;
  bogReferenceNumber?: string;
  totalRecords: number;
  createdAt: string;
}

export default function RegulatoryReports() {
  const [returns, setReturns] = useState<RegulatoryReturn[]>([]);
  const [selectedReturn, setSelectedReturn] = useState<string>('');
  const [reportDate, setReportDate] = useState(new Date().toISOString().split('T')[0]);
  const [loading, setLoading] = useState(false);
  const [generating, setGenerating] = useState(false);

  useEffect(() => {
    fetchRegulatoryReturns();
  }, []);

  const fetchRegulatoryReturns = async () => {
    setLoading(true);
    try {
      // Mock data for demonstration
      const mockReturns: RegulatoryReturn[] = [
        {
          id: 1,
          returnType: 'DailyPosition',
          returnDate: '2025-02-24',
          submissionStatus: 'Draft',
          totalRecords: 15,
          createdAt: '2025-02-24'
        },
        {
          id: 2,
          returnType: 'MonthlyReturn1',
          returnDate: '2025-01-31',
          submissionStatus: 'Submitted',
          bogReferenceNumber: 'BOG-20250201-A1B2C3D4',
          totalRecords: 1203,
          createdAt: '2025-02-01'
        },
        {
          id: 3,
          returnType: 'PrudentialReturn',
          returnDate: '2025-02-24',
          submissionStatus: 'Draft',
          totalRecords: 42,
          createdAt: '2025-02-24'
        }
      ];
      setReturns(mockReturns);
    } catch (error) {
      console.error('Error fetching regulatory returns:', error);
    }
    setLoading(false);
  };

  const handleGenerateReturn = async (returnType: string) => {
    setGenerating(true);
    try {
      // In real implementation, call API endpoint
      const token = localStorage.getItem('token');
      // const response = await fetch(`/api/regulatory-reports/${returnType}?asOfDate=${reportDate}`, {
      //   headers: { Authorization: `Bearer ${token}` }
      // });
      
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 2000));
      
      // Refresh the list
      fetchRegulatoryReturns();
    } catch (error) {
      console.error('Error generating return:', error);
    }
    setGenerating(false);
  };

  const statusIcons: Record<string, React.ReactNode> = {
    'Draft': <Clock className="w-5 h-5 text-yellow-600" />,
    'Submitted': <CheckCircle className="w-5 h-5 text-green-600" />,
    'Rejected': <AlertCircle className="w-5 h-5 text-red-600" />
  };

  const statusColors: Record<string, string> = {
    'Draft': 'bg-yellow-50 border-yellow-200',
    'Submitted': 'bg-green-50 border-green-200',
    'Rejected': 'bg-red-50 border-red-200'
  };

  const regulatoryReportTypes = [
    { id: 'daily-position', name: 'Daily Position Report', frequency: 'Daily' },
    { id: 'monthly-return-1', name: 'Monthly Return 1 (Deposits)', frequency: 'Monthly' },
    { id: 'monthly-return-2', name: 'Monthly Return 2 (Loans)', frequency: 'Monthly' },
    { id: 'monthly-return-3', name: 'Monthly Return 3 (Off-BS)', frequency: 'Monthly' },
    { id: 'prudential', name: 'Prudential Return', frequency: 'Quarterly' },
    { id: 'large-exposure', name: 'Large Exposure Report', frequency: 'Monthly' }
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <FileText className="w-6 h-6 text-red-600" />
            Bank of Ghana Regulatory Reports
          </h2>
          <p className="text-gray-600 mt-1">Generate and submit required compliance reports</p>
        </div>
      </div>

      {/* Generate Report Section */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Generate New Report</h3>
        
        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">Report Type</label>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {regulatoryReportTypes.map(report => (
              <button
                key={report.id}
                onClick={() => setSelectedReturn(report.id)}
                className={`text-left p-3 rounded-lg border-2 transition-colors ${
                  selectedReturn === report.id
                    ? 'bg-blue-50 border-blue-500'
                    : 'bg-gray-50 border-gray-200 hover:border-gray-300'
                }`}
              >
                <p className="font-medium text-gray-900">{report.name}</p>
                <p className="text-xs text-gray-600">{report.frequency}</p>
              </button>
            ))}
          </div>
        </div>

        <div className="mb-4">
          <label className="block text-sm font-medium text-gray-700 mb-2">Report As Of Date</label>
          <input
            type="date"
            value={reportDate}
            onChange={e => setReportDate(e.target.value)}
            className="w-full px-3 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <button
          onClick={() => handleGenerateReturn(selectedReturn)}
          disabled={!selectedReturn || generating}
          className="bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 text-white px-6 py-2 rounded-lg font-medium transition-colors"
        >
          {generating ? 'Generating...' : 'Generate Report'}
        </button>
      </div>

      {/* Reports List */}
      <div className="space-y-4">
        <h3 className="text-lg font-semibold text-gray-900">Recent Reports</h3>

        {loading ? (
          <div className="text-center py-12">
            <div className="animate-spin inline-block w-8 h-8 border-4 border-blue-200 border-t-blue-600 rounded-full"></div>
          </div>
        ) : (
          <div className="space-y-3">
            {returns.map(ret => (
              <div key={ret.id} className={`rounded-lg border-2 p-4 ${statusColors[ret.submissionStatus] || 'bg-gray-50 border-gray-200'}`}>
                <div className="flex items-start justify-between">
                  <div className="flex items-start gap-3 flex-1">
                    <div className="mt-1">
                      {statusIcons[ret.submissionStatus]}
                    </div>
                    <div className="flex-1">
                      <div className="flex items-center gap-2">
                        <h4 className="font-semibold text-gray-900">{ret.returnType}</h4>
                        <span className={`text-xs font-medium px-2 py-1 rounded-full ${
                          ret.submissionStatus === 'Submitted'
                            ? 'bg-green-100 text-green-800'
                            : ret.submissionStatus === 'Draft'
                            ? 'bg-yellow-100 text-yellow-800'
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {ret.submissionStatus}
                        </span>
                      </div>
                      <p className="text-sm text-gray-600 mt-1">
                        As of: {new Date(ret.returnDate).toLocaleDateString()} • Records: {ret.totalRecords}
                      </p>
                      {ret.bogReferenceNumber && (
                        <p className="text-xs text-gray-700 mt-2">
                          <span className="font-medium">BoG Reference:</span> {ret.bogReferenceNumber}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-2 ml-4">
                    {ret.submissionStatus === 'Draft' && (
                      <>
                        <button className="p-2 hover:bg-blue-100 rounded-lg transition-colors">
                          <Download className="w-5 h-5 text-blue-600" />
                        </button>
                        <button className="p-2 hover:bg-green-100 rounded-lg transition-colors">
                          <Send className="w-5 h-5 text-green-600" />
                        </button>
                      </>
                    )}
                    {ret.submissionStatus === 'Submitted' && (
                      <button className="p-2 hover:bg-blue-100 rounded-lg transition-colors">
                        <Download className="w-5 h-5 text-blue-600" />
                      </button>
                    )}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Information Box */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <p className="text-sm text-blue-900">
          <strong>Note:</strong> All regulatory reports must be submitted within the specified deadlines. 
          Ensure data accuracy before submission. Bank of Ghana will provide a reference number upon successful submission.
        </p>
      </div>
    </div>
  );
}
