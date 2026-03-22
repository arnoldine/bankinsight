import React from 'react';
import { AIInsight } from '../types';
import { AlertTriangle, TrendingUp, Lightbulb, Loader2 } from 'lucide-react';

interface InsightPanelProps {
  insights: AIInsight[];
  loading: boolean;
  onRefresh: () => void;
}

const InsightPanel: React.FC<InsightPanelProps> = ({ insights, loading, onRefresh }) => {
  return (
    <div className="bg-white rounded-xl shadow-sm border border-gray-100 h-full flex flex-col">
      <div className="p-4 border-b border-gray-100 flex justify-between items-center">
        <h2 className="text-lg font-semibold text-gray-800 flex items-center gap-2">
          <Lightbulb className="text-yellow-500" size={20} />
          BankInsight Engine
        </h2>
        <button 
          onClick={onRefresh}
          disabled={loading}
          className="text-sm text-indigo-600 hover:text-indigo-800 font-medium flex items-center gap-1 disabled:opacity-50"
        >
          {loading ? <Loader2 className="animate-spin" size={14} /> : null}
          {loading ? 'Analyzing...' : 'Refresh Insights'}
        </button>
      </div>

      <div className="p-4 space-y-4 overflow-y-auto flex-1">
        {loading && insights.length === 0 ? (
          <div className="flex flex-col items-center justify-center h-40 text-gray-400">
             <Loader2 className="animate-spin mb-2" size={32} />
             <p>Running deep analysis...</p>
          </div>
        ) : (
          insights.map((insight, idx) => (
            <div key={idx} className={`p-4 rounded-lg border-l-4 ${
              insight.severity === 'high' ? 'bg-red-50 border-red-500' :
              insight.severity === 'medium' ? 'bg-orange-50 border-orange-500' :
              'bg-blue-50 border-blue-500'
            }`}>
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-2 mb-1">
                  {insight.type === 'fraud' && <AlertTriangle size={16} className="text-red-600" />}
                  {insight.type === 'liquidity' && <TrendingUp size={16} className="text-blue-600" />}
                  {insight.type === 'compliance' && <Lightbulb size={16} className="text-orange-600" />}
                  <span className="font-semibold text-gray-900 text-sm uppercase tracking-wide">{insight.type}</span>
                </div>
                <span className={`text-xs px-2 py-0.5 rounded-full font-medium border ${
                    insight.severity === 'high' ? 'border-red-200 text-red-700' :
                    insight.severity === 'medium' ? 'border-orange-200 text-orange-700' :
                    'border-blue-200 text-blue-700'
                }`}>
                  {insight.severity} Priority
                </span>
              </div>
              <h3 className="text-gray-900 font-medium mb-1">{insight.title}</h3>
              <p className="text-gray-600 text-sm leading-relaxed">
                {insight.description}
              </p>
            </div>
          ))
        )}
        {!loading && insights.length === 0 && (
            <div className="text-center text-gray-500 py-10">
                Ready to analyze data. Click refresh.
            </div>
        )}
      </div>
    </div>
  );
};

export default InsightPanel;