import React, { useState, useEffect, useMemo } from 'react';
import { useTreasury, useInvestments } from '../hooks/useApi';
import {
  TrendingUp,
  DollarSign,
  Activity,
  ArrowUpRight,
  ArrowDownLeft,
  Loader,
  AlertCircle,
  Plus,
  RefreshCw,
  Search,
  Filter,
} from 'lucide-react';

interface TreasuryPosition {
  id: string | number;
  currency: string;
  positionDate: string;
  openingBalance: number;
  closingBalance: number;
  deposits: number;
  withdrawals: number;
  positionStatus: string;
}

interface FxTrade {
  id: string | number;
  dealNumber: string;
  baseCurrency: string;
  counterCurrency: string;
  baseAmount: number;
  counterAmount: number;
  exchangeRate: number;
  tradeDate: string;
  status: string;
}

interface TreasuryManagementHubProps {
  investments?: any[];
  fixedDeposits?: any[];
  onCreateInvestment?: (data: any) => void;
  onCreateFixedDeposit?: (data: any) => void;
  onLiquidateInvestment?: (id: string) => void;
  initialTab?: 'positions' | 'trades' | 'investments' | 'deposits';
}

export default function TreasuryManagementHub({ 
  investments: initialInvestments = [], 
  fixedDeposits: initialFixedDeposits = [],
  onCreateInvestment,
  onCreateFixedDeposit,
  onLiquidateInvestment,
  initialTab = 'positions'
}: TreasuryManagementHubProps) {
  const { getTreasuryPositions, getFxTrades, createFxTrade, error } = useTreasury();
  const investmentsApi = useInvestments();
  const [positions, setPositions] = useState<TreasuryPosition[]>([]);
  const [trades, setTrades] = useState<FxTrade[]>([]);
  const [investments, setInvestments] = useState(initialInvestments || []);
  const [fixedDeposits, setFixedDeposits] = useState(initialFixedDeposits || []);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'positions' | 'trades' | 'investments' | 'deposits'>(initialTab);
  const [showNewTradeForm, setShowNewTradeForm] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [currencyFilter, setCurrencyFilter] = useState('ALL');

  useEffect(() => {
    setActiveTab(initialTab);
  }, [initialTab]);

  useEffect(() => {
    loadTreasuryData();
    if (!initialInvestments || initialInvestments.length === 0) {
      loadInvestments();
    }
    if (!initialFixedDeposits || initialFixedDeposits.length === 0) {
      loadFixedDeposits();
    }
  }, []);

  const loadTreasuryData = async () => {
    try {
      setLoading(true);
      const [posData, tradeData] = await Promise.all([
        getTreasuryPositions(),
        getFxTrades(),
      ]);

      if (posData) setPositions(Array.isArray(posData) ? (posData as any) : []);
      if (tradeData) setTrades(Array.isArray(tradeData) ? (tradeData as any) : []);
    } catch (err) {
      console.error('Failed to load treasury data:', err);
    } finally {
      setLoading(false);
    }
  };

  const loadInvestments = async () => {
    try {
      const data = await investmentsApi.getInvestments();
      if (data) setInvestments(data);
    } catch (err) {
      console.error('Failed to load investments:', err);
    }
  };

  const loadFixedDeposits = async () => {
    try {
      const data = await investmentsApi.getFixedDeposits();
      if (data) setFixedDeposits(data);
    } catch (err) {
      console.error('Failed to load fixed deposits:', err);
    }
  };

  const totalMarketValue = positions.reduce((sum, p) => sum + (p.closingBalance || 0), 0);
  const totalTradesVolume = trades.reduce(
    (sum, t) => sum + (t.counterAmount || 0),
    0
  );
  const availableCurrencies = useMemo(
    () => [
      'ALL',
      ...Array.from(
        new Set(
          [
            ...positions.map((position) => position.currency),
            ...trades.flatMap((trade) => [trade.baseCurrency, trade.counterCurrency]),
          ].filter(Boolean)
        )
      ).sort(),
    ],
    [positions, trades]
  );
  const normalizedQuery = searchQuery.trim().toLowerCase();
  const filteredPositions = useMemo(
    () => positions.filter((position) => {
      if (currencyFilter !== 'ALL' && position.currency !== currencyFilter) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [String(position.id), position.currency, position.positionStatus]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(normalizedQuery);
    }),
    [positions, currencyFilter, normalizedQuery]
  );
  const filteredTrades = useMemo(
    () => trades.filter((trade) => {
      if (
        currencyFilter !== 'ALL' &&
        trade.baseCurrency !== currencyFilter &&
        trade.counterCurrency !== currencyFilter
      ) {
        return false;
      }

      if (!normalizedQuery) {
        return true;
      }

      return [String(trade.id), trade.dealNumber, trade.baseCurrency, trade.counterCurrency, trade.status]
        .filter(Boolean)
        .join(' ')
        .toLowerCase()
        .includes(normalizedQuery);
    }),
    [trades, currencyFilter, normalizedQuery]
  );
  const atRiskPositions = filteredPositions.filter((position) => {
    const status = (position.positionStatus || '').toUpperCase();
    return status.includes('RISK') || status.includes('WATCH') || status.includes('BREACH') || status.includes('LIMIT');
  }).length;

  return (
    <div className="simple-screen p-6 space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold mb-2">Treasury Management</h2>
          <p className="text-gray-500 dark:text-slate-400">Monitor positions, execute FX trades, manage investments</p>
        </div>
        <button
          onClick={loadTreasuryData}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 transition-colors"
        >
          <RefreshCw className="w-4 h-4" />
          Refresh
        </button>
      </div>

      {/* Error Alert */}
      {error && (
        <div className="p-4 bg-red-900/20 border border-red-500/50 rounded-lg flex items-start gap-3">
          <AlertCircle className="w-5 h-5 text-red-400 flex-shrink-0 mt-0.5" />
          <p className="text-red-200">{error}</p>
        </div>
      )}

      <div className="screen-hero p-6">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-end xl:justify-between">
          <div>
            <p className="text-xs font-semibold text-slate-500">Treasury operations</p>
            <h3 className="mt-2 text-2xl font-semibold text-slate-900">Monitor liquidity, FX activity, and investments.</h3>
          </div>
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Net position</p><p className="mt-1 text-xl font-semibold text-slate-900">${totalMarketValue.toLocaleString('en-US', { maximumFractionDigits: 0 })}</p></div>
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">FX volume</p><p className="mt-1 text-xl font-semibold text-slate-900">${totalTradesVolume.toLocaleString('en-US', { maximumFractionDigits: 0 })}</p></div>
            <div className="screen-stat px-4 py-3"><p className="text-xs text-slate-500">Watchlist</p><p className="mt-1 text-xl font-semibold text-slate-900">{atRiskPositions} positions</p></div>
          </div>
        </div>
      </div>

      {/* KPIs */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Total Market Value</p>
          <p className="text-2xl font-bold text-blue-400">
            ${totalMarketValue.toLocaleString('en-US', { maximumFractionDigits: 0 })}
          </p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">{positions.length} positions</p>
        </div>

        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Trading Volume</p>
          <p className="text-2xl font-bold text-green-400">
            ${totalTradesVolume.toLocaleString('en-US', { maximumFractionDigits: 0 })}
          </p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">{trades.length} trades</p>
        </div>

        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Active Currencies</p>
          <p className="text-2xl font-bold text-purple-400">
            {new Set(positions.map((p) => p.currency)).size}
          </p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">In use</p>
        </div>

        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">System Status</p>
          <p className="text-2xl font-bold text-green-400">Operational</p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">All systems normal</p>
        </div>
      </div>

      <div className="rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
        <div className="grid grid-cols-1 gap-3 xl:grid-cols-[1fr_220px]">
          <label className="relative block">
            <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
            <input
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              placeholder="Search currency, deal number, position ID, or status"
              className="screen-input pl-10"
            />
          </label>
          <label className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3">
            <Filter className="h-4 w-4 text-slate-400" />
            <select value={currencyFilter} onChange={(event) => setCurrencyFilter(event.target.value)} className="w-full bg-transparent py-3 text-sm text-slate-700 outline-none">
              {availableCurrencies.map((currency) => (<option key={currency} value={currency}>{currency === 'ALL' ? 'All currencies' : currency}</option>))}
            </select>
          </label>
        </div>
        <div className="mt-3 flex flex-wrap gap-2 text-xs text-slate-500">
          <span className="rounded-full bg-slate-100 px-3 py-1">Positions: {filteredPositions.length}</span>
          <span className="rounded-full bg-slate-100 px-3 py-1">Trades: {filteredTrades.length}</span>
          {currencyFilter !== 'ALL' && <span className="rounded-full bg-blue-50 px-3 py-1 text-blue-700">Currency: {currencyFilter}</span>}
          {searchQuery && <span className="rounded-full bg-amber-50 px-3 py-1 text-amber-700">Search: {searchQuery}</span>}
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-2 border-b border-gray-200 dark:border-slate-700 overflow-x-auto">
        <button
          onClick={() => setActiveTab('positions')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors whitespace-nowrap ${activeTab === 'positions'
            ? 'border-blue-500 text-blue-400'
            : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-600 dark:text-slate-300'
            }`}
        >
          <TrendingUp className="w-4 h-4 inline mr-2" />
          Positions ({positions.length})
        </button>
        <button
          onClick={() => setActiveTab('trades')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors whitespace-nowrap ${activeTab === 'trades'
            ? 'border-blue-500 text-blue-400'
            : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-600 dark:text-slate-300'
            }`}
        >
          <DollarSign className="w-4 h-4 inline mr-2" />
          FX Trades ({trades.length})
        </button>
        <button
          onClick={() => setActiveTab('investments')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors whitespace-nowrap ${activeTab === 'investments'
            ? 'border-blue-500 text-blue-400'
            : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-600 dark:text-slate-300'
            }`}
        >
          <TrendingUp className="w-4 h-4 inline mr-2" />
          Investments ({investments.length})
        </button>
        <button
          onClick={() => setActiveTab('deposits')}
          className={`px-4 py-2 font-medium border-b-2 transition-colors whitespace-nowrap ${activeTab === 'deposits'
            ? 'border-blue-500 text-blue-400'
            : 'border-transparent text-gray-500 dark:text-slate-400 hover:text-gray-600 dark:text-slate-300'
            }`}
        >
          <Activity className="w-4 h-4 inline mr-2" />
          Fixed Deposits ({fixedDeposits.length})
        </button>
      </div>

      {/* Content */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader className="w-8 h-8 animate-spin text-blue-400" />
        </div>
      ) : activeTab === 'positions' ? (
        <PositionsView positions={filteredPositions} />
      ) : activeTab === 'trades' ? (
        <TradesView trades={filteredTrades} onNewTrade={() => setShowNewTradeForm(true)} />
      ) : activeTab === 'investments' ? (
        <InvestmentsView 
          investments={investments}
          onCreateInvestment={onCreateInvestment}
          onLiquidateInvestment={onLiquidateInvestment}
        />
      ) : (
        <FixedDepositsView
          fixedDeposits={fixedDeposits}
          onCreateFixedDeposit={onCreateFixedDeposit}
        />
      )}

      {/* New Trade Form Modal */}
      {showNewTradeForm && (
        <NewTradeForm createFxTrade={createFxTrade} onClose={() => setShowNewTradeForm(false)} />
      )}
    </div>
  );
}

/**
 * Positions View Component
 */
function PositionsView({ positions }: { positions: TreasuryPosition[] }) {
  if (positions.length === 0) {
    return (
      <div className="text-center py-12">
        <TrendingUp className="w-12 h-12 text-gray-400 dark:text-slate-500 mx-auto mb-3" />
        <p className="text-gray-500 dark:text-slate-400">No positions found</p>
      </div>
    );
  }

  // Group by currency
  const grouped = positions.reduce(
    (acc, pos) => {
      const cls = pos.currency || 'Other';
      if (!acc[cls]) acc[cls] = [];
      acc[cls].push(pos);
      return acc;
    },
    {} as Record<string, TreasuryPosition[]>
  );

  return (
    <div className="space-y-4">
      {Object.entries(grouped).map(([currencyGroup, assetPositions]) => (
        <div
          key={currencyGroup}
          className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden"
        >
          <div className="p-4 bg-gray-50 dark:bg-slate-700/50 border-b border-gray-200 dark:border-slate-700 font-semibold flex justify-between">
            <span>{currencyGroup}</span>
            <span>
              ${assetPositions
                .reduce((sum, p) => sum + (p.closingBalance || 0), 0)
                .toLocaleString('en-US', { maximumFractionDigits: 0 })}
            </span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="text-sm text-gray-500 dark:text-slate-400 border-b border-gray-200 dark:border-slate-700">
                <tr className="bg-gray-50 dark:bg-slate-700/30">
                  <th className="px-4 py-3 text-left">ID</th>
                  <th className="px-4 py-3 text-left">Currency</th>
                  <th className="px-4 py-3 text-right">Opening Balance</th>
                  <th className="px-4 py-3 text-right">Closing Balance</th>
                  <th className="px-4 py-3 text-right">Change</th>
                </tr>
              </thead>
              <tbody>
                {assetPositions.map((pos) => (
                  <tr
                    key={pos.id}
                    className="border-b border-gray-200 dark:border-slate-700/50 hover:bg-gray-50 dark:bg-slate-700/30 transition-colors"
                  >
                    <td className="px-4 py-3 font-mono text-sm text-gray-500 dark:text-slate-400">{pos.id}</td>
                    <td className="px-4 py-3 font-medium">{pos.currency}</td>
                    <td className="px-4 py-3 text-right">
                      {pos.openingBalance?.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-4 py-3 text-right text-green-400">
                      ${pos.closingBalance?.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <span
                        className={`flex items-center justify-end gap-1 ${((pos.closingBalance || 0) - (pos.openingBalance || 0)) >= 0
                          ? 'text-green-400'
                          : 'text-red-400'
                          }`}
                      >
                        {((pos.closingBalance || 0) - (pos.openingBalance || 0)) >= 0 ? (
                          <ArrowUpRight className="w-3 h-3" />
                        ) : (
                          <ArrowDownLeft className="w-3 h-3" />
                        )}
                        {((((pos.closingBalance || 0) - (pos.openingBalance || 0)) / (pos.openingBalance || 1)) * 100).toFixed(2)}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="px-4 py-3 bg-gray-50 dark:bg-slate-700/20 border-t border-gray-200 dark:border-slate-700 text-right">
            <p className="text-sm text-gray-500 dark:text-slate-400">
              Subtotal:{' '}
              <span className="font-semibold text-gray-900 dark:text-white">
                ${assetPositions
                  .reduce((sum, p) => sum + p.closingBalance, 0)
                  .toLocaleString('en-US', { maximumFractionDigits: 2 })}
              </span>
            </p>
          </div>
        </div>
      ))}
    </div>
  );
}

/**
 * Investments View Component
 */
function InvestmentsView({
  investments,
  onCreateInvestment,
  onLiquidateInvestment,
}: {
  investments: any[];
  onCreateInvestment?: (data: any) => void;
  onLiquidateInvestment?: (id: string) => void;
}) {
  if (!investments || investments.length === 0) {
    return (
      <div className="text-center py-12">
        <TrendingUp className="w-12 h-12 text-gray-400 dark:text-slate-500 mx-auto mb-3" />
        <p className="text-gray-500 dark:text-slate-400">No investments found</p>
        {onCreateInvestment && (
          <button
            onClick={() => onCreateInvestment({})}
            className="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 mx-auto transition-colors"
          >
            <Plus className="w-4 h-4" />
            Create Investment
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {onCreateInvestment && (
        <button
          onClick={() => onCreateInvestment({})}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 transition-colors"
        >
          <Plus className="w-4 h-4" />
          Create Investment
        </button>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Total Investments</p>
          <p className="text-2xl font-bold text-blue-400">${investments.reduce((sum, inv) => sum + (inv.principal || 0), 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}</p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">{investments.length} active</p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Accrued Interest</p>
          <p className="text-2xl font-bold text-green-400">${investments.reduce((sum, inv) => sum + (inv.accruedInterest || 0), 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}</p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Avg Yield</p>
          <p className="text-2xl font-bold text-purple-400">{(investments.reduce((sum, inv) => sum + (inv.yieldRate || 0), 0) / investments.length).toFixed(2)}%</p>
        </div>
      </div>

      <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="text-sm text-gray-500 dark:text-slate-400 border-b border-gray-200 dark:border-slate-700">
              <tr className="bg-gray-50 dark:bg-slate-700/30">
                <th className="px-4 py-3 text-left">Investment ID</th>
                <th className="px-4 py-3 text-left">Type</th>
                <th className="px-4 py-3 text-right">Principal</th>
                <th className="px-4 py-3 text-right">Yield %</th>
                <th className="px-4 py-3 text-right">Maturity Value</th>
                <th className="px-4 py-3 text-left">Maturity Date</th>
                <th className="px-4 py-3 text-center">Status</th>
                <th className="px-4 py-3 text-center">Actions</th>
              </tr>
            </thead>
            <tbody>
              {investments.map((inv) => (
                <tr key={inv.id} className="hover:bg-gray-50 dark:bg-slate-700/20 border-b border-gray-200 dark:border-slate-700">
                  <td className="py-3 px-4 font-mono text-sm text-gray-500 dark:text-slate-400">
                    {inv.id?.toString().substring(0, 8)}...
                  </td>
                  <td className="py-3 px-4 font-medium">{inv.investmentType || 'Bond'}</td>
                  <td className="py-3 px-4 text-right">
                    ${(inv.principal || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}
                  </td>
                  <td className="py-3 px-4 text-right">{inv.yieldRate?.toFixed(2) || '0.00'}%</td>
                  <td className="py-3 px-4 text-right text-green-400">
                    ${(inv.maturityValue || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}
                  </td>
                  <td className="py-3 px-4 text-gray-500 dark:text-slate-400">
                    {new Date(inv.maturityDate).toLocaleDateString()}
                  </td>
                  <td className="py-3 px-4 text-center">
                    <span className="px-2 py-1 rounded text-xs bg-green-900/40 text-green-400">
                      Active
                    </span>
                  </td>
                  <td className="py-3 px-4 text-center">
                    {onLiquidateInvestment && (
                      <button
                        onClick={() => onLiquidateInvestment(inv.id)}
                        className="text-red-400 hover:text-red-300 text-sm transition-colors"
                      >
                        Liquidate
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

/**
 * Fixed Deposits View Component
 */
function FixedDepositsView({
  fixedDeposits,
  onCreateFixedDeposit,
}: {
  fixedDeposits: any[];
  onCreateFixedDeposit?: (data: any) => void;
}) {
  if (!fixedDeposits || fixedDeposits.length === 0) {
    return (
      <div className="text-center py-12">
        <Activity className="w-12 h-12 text-gray-400 dark:text-slate-500 mx-auto mb-3" />
        <p className="text-gray-500 dark:text-slate-400">No fixed deposits found</p>
        {onCreateFixedDeposit && (
          <button
            onClick={() => onCreateFixedDeposit({})}
            className="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 mx-auto transition-colors"
          >
            <Plus className="w-4 h-4" />
            Create Fixed Deposit
          </button>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {onCreateFixedDeposit && (
        <button
          onClick={() => onCreateFixedDeposit({})}
          className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 transition-colors"
        >
          <Plus className="w-4 h-4" />
          Create Fixed Deposit
        </button>
      )}

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Total Principal</p>
          <p className="text-2xl font-bold text-blue-400">${fixedDeposits.reduce((sum, fd) => sum + (fd.principal || 0), 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}</p>
          <p className="text-xs text-gray-400 dark:text-slate-500 mt-2">{fixedDeposits.length} active</p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Accrued Interest</p>
          <p className="text-2xl font-bold text-green-400">${fixedDeposits.reduce((sum, fd) => sum + (fd.accruedInterest || 0), 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}</p>
        </div>
        <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg p-4">
          <p className="text-gray-500 dark:text-slate-400 text-sm mb-1">Avg Rate</p>
          <p className="text-2xl font-bold text-purple-400">{(fixedDeposits.reduce((sum, fd) => sum + (fd.interestRate || 0), 0) / fixedDeposits.length).toFixed(2)}%</p>
        </div>
      </div>

      <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="text-sm text-gray-500 dark:text-slate-400 border-b border-gray-200 dark:border-slate-700">
              <tr className="bg-gray-50 dark:bg-slate-700/30">
                <th className="px-4 py-3 text-left">FD ID</th>
                <th className="px-4 py-3 text-right">Principal</th>
                <th className="px-4 py-3 text-right">Rate %</th>
                <th className="px-4 py-3 text-left">Tenure</th>
                <th className="px-4 py-3 text-right">Maturity Amount</th>
                <th className="px-4 py-3 text-left">Maturity Date</th>
                <th className="px-4 py-3 text-center">Status</th>
              </tr>
            </thead>
            <tbody>
              {fixedDeposits.map((fd) => (
                <tr key={fd.id} className="hover:bg-gray-50 dark:bg-slate-700/20 border-b border-gray-200 dark:border-slate-700">
                  <td className="py-3 px-4 font-mono text-sm text-gray-500 dark:text-slate-400">
                    {fd.id?.toString().substring(0, 8)}...
                  </td>
                  <td className="py-3 px-4 text-right">
                    ${(fd.principal || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}
                  </td>
                  <td className="py-3 px-4 text-right">{fd.interestRate?.toFixed(2) || '0.00'}%</td>
                  <td className="py-3 px-4">{fd.tenure || '12 months'}</td>
                  <td className="py-3 px-4 text-right text-green-400">
                    ${(fd.maturityAmount || 0).toLocaleString('en-US', { maximumFractionDigits: 2 })}
                  </td>
                  <td className="py-3 px-4 text-gray-500 dark:text-slate-400">
                    {new Date(fd.maturityDate).toLocaleDateString()}
                  </td>
                  <td className="py-3 px-4 text-center">
                    <span className="px-2 py-1 rounded text-xs bg-blue-900/40 text-blue-400">
                      Active
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

/**
 * Trades View Component
 */
function TradesView({
  trades,
  onNewTrade,
}: {
  trades: FxTrade[];
  onNewTrade: () => void;
}) {
  if (trades.length === 0) {
    return (
      <div className="text-center py-12">
        <DollarSign className="w-12 h-12 text-gray-400 dark:text-slate-500 mx-auto mb-3" />
        <p className="text-gray-500 dark:text-slate-400">No trades found</p>
        <button
          onClick={onNewTrade}
          className="mt-4 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 mx-auto transition-colors"
        >
          <Plus className="w-4 h-4" />
          Create Trade
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <button
        onClick={onNewTrade}
        className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg flex items-center gap-2 transition-colors"
      >
        <Plus className="w-4 h-4" />
        New Trade
      </button>

      <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="text-sm text-gray-500 dark:text-slate-400 border-b border-gray-200 dark:border-slate-700">
              <tr className="bg-gray-50 dark:bg-slate-700/30">
                <th className="px-4 py-3 text-left">Trade ID</th>
                <th className="px-4 py-3 text-left">Currency Pair</th>
                <th className="px-4 py-3 text-right">From Amount</th>
                <th className="px-4 py-3 text-right">Rate</th>
                <th className="px-4 py-3 text-right">To Amount</th>
                <th className="px-4 py-3 text-left">Date</th>
                <th className="px-4 py-3 text-center">Status</th>
              </tr>
            </thead>
            <tbody>
              {trades.map((trade) => (
                <tr key={trade.id} className="hover:bg-gray-50 dark:bg-slate-700/20">
                  <td className="py-3 px-4 font-mono text-sm text-gray-500 dark:text-slate-400">
                    {(trade.dealNumber || trade.id).toString().substring(0, 8)}...
                  </td>
                  <td className="py-3 px-4 font-medium">
                    {trade.baseCurrency}/{trade.counterCurrency}
                  </td>
                  <td className="py-3 px-4 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <ArrowDownLeft className="w-4 h-4 text-red-400" />
                      {trade.baseAmount?.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                    </div>
                  </td>
                  <td className="py-3 px-4 text-right text-gray-500 dark:text-slate-400">
                    {trade.exchangeRate?.toFixed(4)}
                  </td>
                  <td className="py-3 px-4 text-right">
                    <div className="flex items-center justify-end gap-1">
                      <ArrowUpRight className="w-4 h-4 text-green-400" />
                      <span className="text-green-400">
                        {trade.counterAmount?.toLocaleString('en-US', { maximumFractionDigits: 2 })}
                      </span>
                    </div>
                  </td>
                  <td className="py-3 px-4 text-gray-500 dark:text-slate-400">
                    {new Date(trade.tradeDate).toLocaleDateString()}
                  </td>
                  <td className="py-3 px-4 text-center">
                    <span
                      className={`px-2 py-1 rounded inline-block text-xs ${trade.status?.toUpperCase() === 'SETTLED'
                        ? 'bg-green-900/40 text-green-400'
                        : trade.status?.toUpperCase() === 'PENDING'
                          ? 'bg-yellow-900/40 text-yellow-400'
                          : 'bg-gray-50 dark:bg-slate-700 text-gray-600 dark:text-slate-300'
                        }`}
                    >
                      {trade.status}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

/**
 * New Trade Form Modal
 */
function NewTradeForm({ onClose, createFxTrade }: { onClose: () => void; createFxTrade: (trade: any) => Promise<any> }) {
  const [loading, setLoading] = useState(false);
  const [newTrade, setNewTrade] = useState({
    baseCurrency: 'USD',
    counterCurrency: 'EUR',
    baseAmount: 0,
    exchangeRate: 0,
    counterparty: '',
    valueDate: new Date().toISOString().split('T')[0],
  });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      setLoading(true);
      await createFxTrade({
        ...newTrade,
        sellAmount: newTrade.baseAmount * newTrade.exchangeRate,
        tradeType: 'SPOT',
      });
      onClose();
    } catch (err) {
      console.error('Failed to create trade:', err);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-slate-800 border border-gray-200 dark:border-slate-700 rounded-lg max-w-md w-full">
        <div className="p-6 border-b border-gray-200 dark:border-slate-700">
          <h3 className="text-xl font-bold">Create FX Trade</h3>
        </div>

        <form onSubmit={handleSubmit} className="p-6 space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-slate-200 mb-2">
              From Currency
            </label>
            <select
              value={newTrade.baseCurrency}
              onChange={(e) =>
                setNewTrade({ ...newTrade, baseCurrency: e.target.value })
              }
              className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2 text-gray-900 dark:text-white focus:outline-none focus:border-blue-500"
            >
              <option value="USD">USD</option>
              <option value="EUR">EUR</option>
              <option value="GBP">GBP</option>
              <option value="JPY">JPY</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-slate-200 mb-2">
              To Currency
            </label>
            <select
              value={newTrade.counterCurrency}
              onChange={(e) =>
                setNewTrade({ ...newTrade, counterCurrency: e.target.value })
              }
              className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2 text-gray-900 dark:text-white focus:outline-none focus:border-blue-500"
            >
              <option value="EUR">EUR</option>
              <option value="USD">USD</option>
              <option value="GBP">GBP</option>
              <option value="JPY">JPY</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-slate-200 mb-2">
              Amount ({newTrade.baseCurrency})
            </label>
            <input
              type="number"
              value={newTrade.baseAmount}
              onChange={(e) =>
                setNewTrade({ ...newTrade, baseAmount: parseFloat(e.target.value) || 0 })
              }
              placeholder="0.00"
              className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2 text-gray-900 dark:text-white focus:outline-none focus:border-blue-500"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-slate-200 mb-2">
              Exchange Rate
            </label>
            <input
              type="number"
              step="0.0001"
              value={newTrade.exchangeRate}
              onChange={(e) =>
                setNewTrade({ ...newTrade, exchangeRate: parseFloat(e.target.value) || 0 })
              }
              placeholder="0.0000"
              className="w-full bg-gray-100 dark:bg-slate-900 border border-gray-200 dark:border-slate-700 rounded-lg px-4 py-2 text-gray-900 dark:text-white focus:outline-none focus:border-blue-500"
            />
          </div>

          <div className="flex gap-3 pt-4">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 px-4 py-2 bg-gray-50 dark:bg-slate-700 hover:bg-gray-200 dark:hover:bg-gray-200 dark:hover:bg-slate-600 text-gray-900 dark:text-white rounded-lg transition-colors"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading || !newTrade.baseAmount || !newTrade.exchangeRate}
              className="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
            >
              {loading ? 'Executing...' : 'Execute Trade'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}


