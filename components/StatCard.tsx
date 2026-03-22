import React from 'react';
import { ArrowUpRight, ArrowDownRight, Minus } from 'lucide-react';
import { InsightMetric } from '../types';

interface StatCardProps {
  metric: InsightMetric;
  icon: React.ReactNode;
  color?: 'blue' | 'green' | 'orange' | 'red' | 'purple';
}

const StatCard: React.FC<StatCardProps> = ({ metric, icon, color = 'blue' }) => {
  const isUp = metric.trend === 'up';
  const isDown = metric.trend === 'down';

  const colorClasses = {
    blue: {
      bg: 'bg-blue-50 dark:bg-blue-900/20',
      text: 'text-blue-600 dark:text-blue-400',
      border: 'border-blue-100 dark:border-blue-900/30',
      iconBg: 'bg-blue-100 dark:bg-blue-900/30',
    },
    green: {
      bg: 'bg-green-50 dark:bg-green-900/20',
      text: 'text-green-600 dark:text-green-400',
      border: 'border-green-100 dark:border-green-900/30',
      iconBg: 'bg-green-100 dark:bg-green-900/30',
    },
    orange: {
      bg: 'bg-orange-50 dark:bg-orange-900/20',
      text: 'text-orange-600 dark:text-orange-400',
      border: 'border-orange-100 dark:border-orange-900/30',
      iconBg: 'bg-orange-100 dark:bg-orange-900/30',
    },
    red: {
      bg: 'bg-red-50 dark:bg-red-900/20',
      text: 'text-red-600 dark:text-red-400',
      border: 'border-red-100 dark:border-red-900/30',
      iconBg: 'bg-red-100 dark:bg-red-900/30',
    },
    purple: {
      bg: 'bg-purple-50 dark:bg-purple-900/20',
      text: 'text-purple-600 dark:text-purple-400',
      border: 'border-purple-100 dark:border-purple-900/30',
      iconBg: 'bg-purple-100 dark:bg-purple-900/30',
    }
  };

  const classes = colorClasses[color];

  return (
    <div className="bg-white dark:bg-drk-850 p-6 rounded-card shadow-card hover:shadow-medium transition-all duration-300 card-hover border border-slate-200 dark:border-drk-700">
      <div className="flex justify-between items-start mb-4">
        <div className={`p-3 rounded-xl ${classes.iconBg} ${classes.text}`}>
          {icon}
        </div>
        <div className="flex items-center gap-1 text-xs font-semibold">
          {isUp && (
            <span className="text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 px-2 py-1 rounded-md flex items-center gap-0.5">
              <ArrowUpRight size={12} />
              {metric.percentage}
            </span>
          )}
          {isDown && (
            <span className="text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/20 px-2 py-1 rounded-md flex items-center gap-0.5">
              <ArrowDownRight size={12} />
              {metric.percentage}
            </span>
          )}
          {metric.trend === 'neutral' && (
            <span className="text-slate-500 dark:text-slate-400 bg-slate-50 dark:bg-slate-700 px-2 py-1 rounded-md flex items-center gap-0.5">
              <Minus size={12} />
              {metric.percentage}
            </span>
          )}
        </div>
      </div>

      <div>
        <p className="text-[11px] font-bold text-slate-500 dark:text-slate-400 uppercase tracking-wider mb-2">
          {metric.label}
        </p>
        <h3 className="text-3xl font-heading font-bold text-slate-900 dark:text-white mb-2">
          {metric.value}
        </h3>
        <p className="text-xs text-slate-500 dark:text-slate-400">vs last period</p>
      </div>
    </div>
  );
};

export default StatCard;