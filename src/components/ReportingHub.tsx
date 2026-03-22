import React, { useEffect, useMemo, useState } from 'react';
import { useReports } from '../hooks/useApi';
import type { CrbDataQualityDashboardDTO, EnterpriseReportCatalogItem, EnterpriseReportDownloadResult, EnterpriseReportExecutionResponse, EnterpriseReportHistoryItem, ReportFilterPresetItem } from '../services/reportService';
import { ChevronLeft, ChevronRight, Download, FileText, Filter, Heart, History, Loader, RefreshCw, Save, Search, ShieldAlert, Sparkles, Star, TableProperties } from 'lucide-react';

type CatalogCategory = 'All' | 'Regulatory Reports' | 'Operational Reports' | 'Credit Bureau Reports' | 'Accounting & Finance Reports' | 'Audit & Control Reports' | 'Management Reports';

const CATEGORIES: CatalogCategory[] = ['All', 'Regulatory Reports', 'Operational Reports', 'Credit Bureau Reports', 'Accounting & Finance Reports', 'Audit & Control Reports', 'Management Reports'];
const PAGE_SIZES = [10, 25, 50, 100];
const EXPORT_PRESENTATION: Record<string, { label: string; helper: string }> = {
  csv: {
    label: 'CSV export',
    helper: 'Best for raw data extracts, bulk review, and downstream processing in other systems.',
  },
  xlsx: {
    label: 'Excel workbook',
    helper: 'Structured workbook export with totals, typed cells, and tabular formatting.',
  },
  pdf: {
    label: 'PDF export',
    helper: 'Print-friendly output with report metadata and tabular layout.',
  },
};


const defaultValueFor = (type: string, raw?: string | null) => raw || (type === 'date' ? new Date().toISOString().slice(0, 10) : '');
const isBlank = (value?: string | null) => !value || !value.trim();
const formatValue = (value: unknown) => value === null || value === undefined || value === '' ? 'N/A' : typeof value === 'number' ? new Intl.NumberFormat('en-GH', { maximumFractionDigits: 2 }).format(value) : String(value);
const getActionMessage = (error: unknown, fallback: string) => error instanceof Error && error.message ? error.message.replace(/^API Error:\s*/i, '') || fallback : fallback;

const downloadBlob = (download: EnterpriseReportDownloadResult, fallbackName: string) => {
  const resolvedFileName = download.fileName || fallbackName;
  const url = URL.createObjectURL(download.blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = resolvedFileName;
  link.rel = 'noopener';
  document.body.appendChild(link);
  link.click();
  window.setTimeout(() => {
    URL.revokeObjectURL(url);
    link.remove();
  }, 250);
  return resolvedFileName;
};

export default function ReportingHub() {
  const { catalogError, getEnterpriseCatalog, executeEnterpriseReport, exportEnterpriseReport, getEnterpriseHistory, getEnterpriseFavorites, addEnterpriseFavorite, removeEnterpriseFavorite, getEnterprisePresets, saveEnterprisePreset, deleteEnterprisePreset, getCrbDataQualityDashboard } = useReports();
  const [catalog, setCatalog] = useState<EnterpriseReportCatalogItem[]>([]);
  const [history, setHistory] = useState<EnterpriseReportHistoryItem[]>([]);
  const [crbQuality, setCrbQuality] = useState<CrbDataQualityDashboardDTO | null>(null);
  const [selectedCategory, setSelectedCategory] = useState<CatalogCategory>('All');
  const [selectedSubCategory, setSelectedSubCategory] = useState('All');
  const [query, setQuery] = useState('');
  const [selectedReport, setSelectedReport] = useState<EnterpriseReportCatalogItem | null>(null);
  const [parameters, setParameters] = useState<Record<string, string>>({});
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [preview, setPreview] = useState<EnterpriseReportExecutionResponse | null>(null);
  const [previewPageSize, setPreviewPageSize] = useState(25);
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');
  const [presets, setPresets] = useState<ReportFilterPresetItem[]>([]);
  const [presetName, setPresetName] = useState('');
  const [activePresetId, setActivePresetId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [running, setRunning] = useState(false);
  const [exporting, setExporting] = useState<string | null>(null);
  const [refreshing, setRefreshing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
  const [downloadNotice, setDownloadNotice] = useState<string | null>(null);

  const loadWorkspace = async (isRefresh = false) => {
    isRefresh ? setRefreshing(true) : setLoading(true);
    try {
      setActionError(null);
      setDownloadNotice(null);
      const [catalogData, historyData, favorites, quality] = await Promise.all([getEnterpriseCatalog(), getEnterpriseHistory(), getEnterpriseFavorites(), getCrbDataQualityDashboard()]);
      const favoriteCodes = new Set(favorites.map((item) => item.reportCode));
      const merged = catalogData.map((item) => ({ ...item, isFavorite: favoriteCodes.has(item.reportCode) || item.isFavorite }));
      setCatalog(merged);
      setHistory(historyData.slice(0, 8));
      setCrbQuality(quality);
      setSelectedReport((current) => current ?? merged[0] ?? null);
    } catch (error) {
      setActionError(getActionMessage(error, 'Unable to load reporting data right now.'));
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => { loadWorkspace(); }, []);
  useEffect(() => { setSelectedSubCategory('All'); }, [selectedCategory]);

  useEffect(() => {
    if (!selectedReport) return;
    const next = selectedReport.parameterSchema.reduce<Record<string, string>>((acc, field) => {
      acc[field.name] = parameters[field.name] ?? defaultValueFor(field.type, field.defaultValue);
      return acc;
    }, {});
    setParameters(next);
    setFieldErrors({});
    setPreview(null);
    setSortColumn(null);
    setSortDirection('asc');
    setActivePresetId(null);
    setActionError(null);
    setDownloadNotice(null);
    getEnterprisePresets(selectedReport.reportCode).then(setPresets).catch(() => setPresets([]));
  }, [selectedReport]);
  const subCategories = useMemo(() => ['All', ...catalog.filter((item) => selectedCategory === 'All' || item.category === selectedCategory).map((item) => item.subCategory).filter((value, index, source) => source.indexOf(value) === index).sort((a, b) => a.localeCompare(b))], [catalog, selectedCategory]);

  const filteredCatalog = useMemo(() => catalog.filter((item) => {
    const matchesCategory = selectedCategory === 'All' || item.category === selectedCategory;
    const matchesSubCategory = selectedSubCategory === 'All' || item.subCategory === selectedSubCategory;
    const fingerprint = `${item.reportName} ${item.reportCode} ${item.subCategory} ${item.description}`.toLowerCase();
    const matchesQuery = !query.trim() || fingerprint.includes(query.trim().toLowerCase());
    return matchesCategory && matchesSubCategory && matchesQuery;
  }), [catalog, query, selectedCategory, selectedSubCategory]);

  useEffect(() => {
    if (!filteredCatalog.length) return;
    if (!selectedReport || !filteredCatalog.some((item) => item.reportCode === selectedReport.reportCode)) {
      setSelectedReport(filteredCatalog[0]);
    }
  }, [filteredCatalog, selectedReport]);

  const summaryCards = useMemo(() => [
    { label: 'Catalog Reports', value: String(catalog.length), icon: FileText },
    { label: 'Favorites', value: String(catalog.filter((item) => item.isFavorite).length), icon: Star },
    { label: 'Recent Runs', value: String(history.length), icon: History },
    { label: 'CRB Failures', value: String(crbQuality?.failedChecks ?? 0), icon: ShieldAlert },
  ], [catalog, history, crbQuality]);

  const appliedFilters = useMemo(() => !preview ? [] as Array<[string, string]> : Object.entries(preview.appliedFilters).filter(([, value]) => !isBlank(String(value ?? ''))) as Array<[string, string]>, [preview]);
  const previewTotalPages = useMemo(() => !preview ? 1 : Math.max(1, Math.ceil(preview.totalRows / Math.max(preview.pageSize, 1))), [preview]);
  const previewWindow = useMemo(() => !preview || preview.totalRows === 0 ? { start: 0, end: 0 } : { start: (preview.page - 1) * preview.pageSize + 1, end: Math.min(preview.page * preview.pageSize, preview.totalRows) }, [preview]);
  const sortedRows = useMemo(() => {
    if (!preview) return [] as Array<Record<string, unknown>>;
    if (!sortColumn) return preview.rows;
    return [...preview.rows].sort((left, right) => {
      const leftValue = left[sortColumn];
      const rightValue = right[sortColumn];
      if (leftValue === rightValue) return 0;
      if (leftValue === null || leftValue === undefined || leftValue === '') return 1;
      if (rightValue === null || rightValue === undefined || rightValue === '') return -1;
      const leftNumber = Number(leftValue);
      const rightNumber = Number(rightValue);
      const bothNumeric = !Number.isNaN(leftNumber) && !Number.isNaN(rightNumber);
      const comparison = bothNumeric ? leftNumber - rightNumber : String(leftValue).localeCompare(String(rightValue), undefined, { numeric: true, sensitivity: 'base' });
      return sortDirection === 'asc' ? comparison : -comparison;
    });
  }, [preview, sortColumn, sortDirection]);

  const validateParameters = () => {
    if (!selectedReport) return false;
    const nextErrors = selectedReport.parameterSchema.reduce<Record<string, string>>((acc, field) => {
      if (field.required && isBlank(parameters[field.name])) {
        acc[field.name] = `${field.label} is required.`;
      }
      return acc;
    }, {});
    setFieldErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) {
      setActionError('Complete the required filters before previewing or exporting this report.');
      return false;
    }
    return true;
  };

  const updateParameter = (name: string, value: string) => {
    setParameters((current) => ({ ...current, [name]: value }));
    setFieldErrors((current) => {
      if (!current[name]) return current;
      const next = { ...current };
      delete next[name];
      return next;
    });
  };

  const executePreview = async (page: number, pageSize: number) => {
    if (!selectedReport) return;
    setRunning(true);
    setActionError(null);
    try {
      const result = await executeEnterpriseReport(selectedReport.reportCode, {
        parameters,
        page,
        pageSize,
        sortBy: selectedReport.defaultSort ?? undefined,
        sortDirection: selectedReport.defaultSort?.includes('desc') ? 'desc' : 'asc',
      });
      setPreview(result);
      setPreviewPageSize(result.pageSize);
      setHistory(await getEnterpriseHistory());
    } catch (error) {
      setActionError(getActionMessage(error, `Unable to preview ${selectedReport.reportName}.`));
    } finally {
      setRunning(false);
    }
  };

  const runReport = async () => { if (!validateParameters()) return; await executePreview(1, previewPageSize); };
  const runExport = async (format: string) => {
    if (!selectedReport || !validateParameters()) return;
    setExporting(format);
    setActionError(null);
    setDownloadNotice(null);
    try {
      const download = await exportEnterpriseReport(selectedReport.reportCode, format, { parameters, page: 1, pageSize: 5000 });
      const fileName = downloadBlob(download, `${selectedReport.reportCode}.${format}`);
      setDownloadNotice(`${fileName} is being handed off to your browser download manager and will save to the browser's default download location unless your browser is configured to ask first.`);
      setHistory(await getEnterpriseHistory());
    } catch (error) {
      setActionError(getActionMessage(error, `Unable to export ${selectedReport.reportName} as ${format.toUpperCase()}.`));
    } finally {
      setExporting(null);
    }
  };

  const resetParameters = () => {
    if (!selectedReport) return;
    const defaults = selectedReport.parameterSchema.reduce<Record<string, string>>((acc, field) => {
      acc[field.name] = defaultValueFor(field.type, field.defaultValue);
      return acc;
    }, {});
    setParameters(defaults);
    setFieldErrors({});
    setActionError(null);
    setPreview(null);
    setSortColumn(null);
    setSortDirection('asc');
  };

  const savePresetItem = async () => {
    if (!selectedReport || !presetName.trim() || !validateParameters()) return;
    try {
      const preset = await saveEnterprisePreset(selectedReport.reportCode, presetName.trim(), parameters);
      setPresetName('');
      setActivePresetId(preset.id);
      setPresets(await getEnterprisePresets(selectedReport.reportCode));
    } catch (error) {
      setActionError(getActionMessage(error, 'Unable to save filter preset.'));
    }
  };

  const toggleFavorite = async (report: EnterpriseReportCatalogItem) => {
    try {
      if (report.isFavorite) await removeEnterpriseFavorite(report.reportCode);
      else await addEnterpriseFavorite(report.reportCode);
      await loadWorkspace(true);
    } catch (error) {
      setActionError(getActionMessage(error, 'Unable to update report favorites.'));
    }
  };

  const openHistoryReport = (reportCode: string) => {
    const match = catalog.find((item) => item.reportCode === reportCode);
    if (!match) {
      setActionError('This report is no longer available in the current catalog.');
      return;
    }
    setSelectedCategory(match.category as CatalogCategory);
    setSelectedSubCategory('All');
    setSelectedReport(match);
    setActionError(null);
  };
  return (
    <div className="space-y-6 p-6">
      <div className="rounded-[28px] border border-gray-200 bg-white p-5 shadow-sm">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-center xl:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.24em] text-gray-500">Reporting</p>
            <h2 className="mt-2 text-2xl font-bold text-gray-900">Enterprise reporting and regulatory exports</h2>
            <p className="mt-2 max-w-3xl text-sm text-gray-600">Review report catalogs, run previews, export results, track execution history, and monitor CRB data quality.</p>
          </div>
          <button type="button" onClick={() => loadWorkspace(true)} className="inline-flex items-center gap-2 rounded-2xl border border-gray-200 px-4 py-2 text-sm font-medium text-gray-600 hover:border-blue-300 hover:text-blue-700"><RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />Refresh reporting data</button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-4">{summaryCards.map((card) => { const Icon = card.icon; return <div key={card.label} className="rounded-3xl border border-gray-200 bg-white p-4 shadow-sm"><div className="flex items-center justify-between"><p className="text-sm font-medium text-gray-500">{card.label}</p><Icon className="h-5 w-5 text-blue-600" /></div><p className="mt-3 text-3xl font-bold text-gray-900">{card.value}</p></div>; })}</div>
      {catalogError && <div className="rounded-3xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">{catalogError}</div>}

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[1.05fr_1.25fr]">
        <div className="space-y-6">
          <div className="space-y-4 rounded-3xl border border-gray-200 bg-white p-5 shadow-sm">
            <div className="flex items-center justify-between gap-3"><div><h3 className="text-lg font-semibold text-gray-900">Report Catalog</h3><p className="text-sm text-gray-500">Grouped across regulatory, operational, CRB, finance, management, and control categories.</p></div><div className="inline-flex items-center gap-2 rounded-2xl bg-gray-50 px-3 py-2 text-sm text-gray-500"><Filter className="h-4 w-4" />{filteredCatalog.length} visible</div></div>
            <label className="relative block"><Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-gray-400" /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search report name, code, or description" className="w-full rounded-2xl border border-gray-200 bg-gray-50 py-3 pl-10 pr-4 text-sm outline-none focus:border-blue-400 focus:bg-white" /></label>
            <div className="flex flex-wrap gap-2">{CATEGORIES.map((category) => <button key={category} type="button" onClick={() => setSelectedCategory(category)} className={`rounded-full px-3 py-1.5 text-xs font-semibold ${selectedCategory === category ? 'bg-slate-900 text-white' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'}`}>{category}</button>)}</div>
            <div className="flex flex-wrap gap-2">{subCategories.map((subCategory) => <button key={subCategory} type="button" onClick={() => setSelectedSubCategory(subCategory)} className={`rounded-full px-3 py-1.5 text-xs font-semibold ${selectedSubCategory === subCategory ? 'bg-blue-100 text-blue-700' : 'bg-gray-100 text-gray-600 hover:bg-gray-200'}`}>{subCategory}</button>)}</div>
            {selectedCategory === 'Credit Bureau Reports' && <div className="rounded-2xl bg-slate-50 p-3 text-sm text-slate-600">Consumer, business, data-quality, and BoG extract reports are grouped in one catalog and can be narrowed using the subcategory chips above.</div>}
            {loading ? <div className="flex justify-center py-10"><Loader className="h-8 w-8 animate-spin text-blue-500" /></div> : <div className="grid max-h-[720px] grid-cols-1 gap-3 overflow-auto pr-1">{filteredCatalog.length === 0 && <div className="rounded-3xl border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-500">No reports match the current search and category filters.</div>}{filteredCatalog.map((report) => <button key={report.reportCode} type="button" onClick={() => setSelectedReport(report)} className={`rounded-3xl border p-4 text-left transition ${selectedReport?.reportCode === report.reportCode ? 'border-blue-500 bg-blue-50' : 'border-gray-200 bg-white hover:border-gray-300'}`}><div className="flex items-start justify-between gap-3"><div><p className="font-semibold text-gray-900">{report.reportName}</p><p className="mt-1 text-xs uppercase tracking-wide text-gray-500">{report.category} â€¢ {report.subCategory}</p></div><button type="button" onClick={(event) => { event.stopPropagation(); toggleFavorite(report); }} className={`rounded-full p-2 ${report.isFavorite ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-slate-500'}`}><Heart className={`h-4 w-4 ${report.isFavorite ? 'fill-current' : ''}`} /></button></div><p className="mt-3 text-sm text-gray-600">{report.description}</p><div className="mt-3 flex flex-wrap gap-2 text-xs"><span className="rounded-full bg-slate-100 px-2.5 py-1 text-slate-600">{report.reportCode}</span>{report.isRegulatory && <span className="rounded-full bg-blue-100 px-2.5 py-1 text-blue-700">Regulatory</span>}{report.requiresApprovalBeforeFinalExport && <span className="rounded-full bg-amber-100 px-2.5 py-1 text-amber-700">Maker-checker</span>}</div></button>)}</div>}
          </div>
          <div className="rounded-3xl border border-gray-200 bg-white p-5 shadow-sm"><div className="mb-4 flex items-center gap-2"><ShieldAlert className="h-5 w-5 text-blue-600" /><h3 className="text-lg font-semibold text-gray-900">CRB Data Quality</h3></div><div className="grid grid-cols-2 gap-3 text-sm"><div className="rounded-2xl bg-gray-50 p-3"><p className="text-gray-500">Total Checks</p><p className="text-xl font-semibold text-gray-900">{crbQuality?.totalChecks ?? 0}</p></div><div className="rounded-2xl bg-gray-50 p-3"><p className="text-gray-500">Failed Checks</p><p className="text-xl font-semibold text-gray-900">{crbQuality?.failedChecks ?? 0}</p></div><div className="rounded-2xl bg-gray-50 p-3"><p className="text-gray-500">Consumer Missing Fields</p><p className="text-xl font-semibold text-gray-900">{crbQuality?.missingMandatoryConsumerFields ?? 0}</p></div><div className="rounded-2xl bg-gray-50 p-3"><p className="text-gray-500">Rejected Records</p><p className="text-xl font-semibold text-gray-900">{crbQuality?.rejectedRecords ?? 0}</p></div></div></div>
        </div>

        <div className="space-y-6">
          <div className="rounded-3xl border border-gray-200 bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between"><div><p className="text-xs uppercase tracking-wide text-gray-500">Selected report</p><h3 className="mt-1 text-xl font-semibold text-gray-900">{selectedReport?.reportName ?? 'Select a report'}</h3><p className="mt-2 text-sm text-gray-600">{selectedReport?.description ?? 'Choose a report from the catalog to configure parameters and preview the result.'}</p>{selectedReport && <p className="mt-3 text-xs text-gray-500">Exports use the current filter values and inherit the same permission checks as the preview.</p>}</div><div className="flex flex-wrap gap-2">{selectedReport?.exportFormats.map((format) => <button key={format} type="button" onClick={() => runExport(format)} disabled={!selectedReport || exporting === format} className="inline-flex items-center gap-2 rounded-2xl border border-gray-200 px-3 py-2 text-sm font-medium text-gray-700 hover:border-blue-300 hover:text-blue-700 disabled:opacity-60">{exporting === format ? <Loader className="h-4 w-4 animate-spin" /> : <Download className="h-4 w-4" />}{format.toUpperCase()}</button>)}</div></div>
            <div className="mt-5 grid grid-cols-1 gap-4 md:grid-cols-2">{selectedReport?.parameterSchema.map((field) => { const hasError = Boolean(fieldErrors[field.name]); return <label key={field.name} className="space-y-2 text-sm"><span className="font-medium text-gray-700">{field.label}{field.required && <span className="ml-1 text-rose-600">*</span>}</span>{field.type === 'select' ? <select value={parameters[field.name] ?? ''} onChange={(event) => updateParameter(field.name, event.target.value)} className={`w-full rounded-2xl px-3 py-3 outline-none focus:bg-white ${hasError ? 'border border-rose-300 bg-rose-50 focus:border-rose-400' : 'border border-gray-200 bg-gray-50 focus:border-blue-400'}`}><option value="">{field.required ? 'Select one' : 'All'}</option>{field.options.map((option) => <option key={option} value={option}>{option}</option>)}</select> : <input type={field.type === 'date' ? 'date' : 'text'} value={parameters[field.name] ?? ''} onChange={(event) => updateParameter(field.name, event.target.value)} placeholder={field.placeholder ?? field.label} className={`w-full rounded-2xl px-3 py-3 outline-none focus:bg-white ${hasError ? 'border border-rose-300 bg-rose-50 focus:border-rose-400' : 'border border-gray-200 bg-gray-50 focus:border-blue-400'}`} />}{hasError && <span className="text-xs text-rose-700">{fieldErrors[field.name]}</span>}</label>; })}</div>
            {actionError && <div className="mt-5 rounded-2xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-800">{actionError}</div>}
            {downloadNotice && <div className="mt-5 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">{downloadNotice}</div>}
            <div className="mt-5 rounded-3xl border border-slate-200 bg-slate-50 p-4">
              <div className="flex items-center gap-2 text-sm font-semibold text-slate-800"><Download className="h-4 w-4 text-blue-600" />Export options</div>
              <p className="mt-2 text-sm text-slate-600">Downloads are triggered through your browser, so files will go to the browser's default Downloads folder unless your browser is configured to prompt for a location.</p>
              <div className="mt-4 grid gap-3 md:grid-cols-3">
                {selectedReport?.exportFormats.map((format) => {
                  const exportMeta = EXPORT_PRESENTATION[format] ?? { label: format.toUpperCase(), helper: 'Export this report in the selected format.' };
                  return (
                    <button
                      key={format}
                      type="button"
                      onClick={() => runExport(format)}
                      disabled={!selectedReport || exporting === format}
                      className="rounded-2xl border border-slate-200 bg-white p-4 text-left transition hover:border-blue-300 hover:shadow-sm disabled:opacity-60"
                    >
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <p className="text-sm font-semibold text-slate-900">{exportMeta.label}</p>
                          <p className="mt-1 text-xs uppercase tracking-wide text-slate-500">.{format}</p>
                        </div>
                        {exporting === format ? <Loader className="h-4 w-4 animate-spin text-blue-600" /> : <Download className="h-4 w-4 text-blue-600" />}
                      </div>
                      <p className="mt-3 text-sm text-slate-600">{exportMeta.helper}</p>
                    </button>
                  );
                })}
              </div>
            </div>
            <div className="mt-5 flex flex-wrap items-center gap-3"><button type="button" onClick={runReport} disabled={!selectedReport || running} className="inline-flex items-center gap-2 rounded-2xl bg-slate-900 px-4 py-3 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-60">{running ? <Loader className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}Preview report</button><button type="button" onClick={resetParameters} disabled={!selectedReport} className="inline-flex items-center gap-2 rounded-2xl border border-gray-200 px-4 py-3 text-sm font-medium text-gray-600 hover:border-gray-300 hover:text-gray-800 disabled:opacity-60">Reset filters</button><div className="flex items-center gap-2 rounded-2xl border border-gray-200 px-3 py-2"><Save className="h-4 w-4 text-gray-500" /><input value={presetName} onChange={(event) => setPresetName(event.target.value)} placeholder="Preset name" className="bg-transparent text-sm outline-none" /><button type="button" onClick={savePresetItem} disabled={!presetName.trim()} className="text-sm font-semibold text-blue-700 disabled:text-gray-400">Save</button></div><div className="flex items-center gap-2 rounded-2xl border border-gray-200 px-3 py-2 text-sm text-gray-600"><span>Rows per page</span><select value={previewPageSize} onChange={async (event) => { const nextPageSize = Number(event.target.value); setPreviewPageSize(nextPageSize); if (preview) { await executePreview(1, nextPageSize); } }} className="bg-transparent outline-none">{PAGE_SIZES.map((size) => <option key={size} value={size}>{size}</option>)}</select></div></div>
            {presets.length > 0 && <div className="mt-4 flex flex-wrap gap-2">{presets.map((preset) => <div key={preset.id} className={`inline-flex items-center gap-2 rounded-full px-3 py-1.5 text-xs ${activePresetId === preset.id ? 'bg-blue-100 text-blue-700' : 'bg-slate-100 text-slate-700'}`}><button type="button" onClick={() => { setParameters(preset.parameters); setFieldErrors({}); setActivePresetId(preset.id); setActionError(null); }}>{preset.presetName}</button><button type="button" onClick={async () => { try { setActionError(null); await deleteEnterprisePreset(preset.id); setPresets(await getEnterprisePresets(selectedReport!.reportCode)); if (activePresetId === preset.id) setActivePresetId(null); } catch (error) { setActionError(getActionMessage(error, 'Unable to delete preset.')); } }}>x</button></div>)}</div>}
          </div>

          <div className="rounded-3xl border border-gray-200 bg-white p-5 shadow-sm"><div className="mb-4 flex items-center gap-2"><TableProperties className="h-5 w-5 text-blue-600" /><h3 className="text-lg font-semibold text-gray-900">Preview</h3></div>{preview ? <div className="space-y-4"><div className="grid grid-cols-1 gap-3 md:grid-cols-3">{preview.summary.map((metric) => <div key={metric.label} className="rounded-2xl bg-gray-50 p-3"><p className="text-xs uppercase tracking-wide text-gray-500">{metric.label}</p><p className="mt-2 text-lg font-semibold text-gray-900">{metric.value}</p></div>)}</div><div className="grid gap-3 lg:grid-cols-[1.2fr_0.8fr]"><div className="flex flex-wrap items-center justify-between gap-3 rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-600"><span>Showing {previewWindow.start}-{previewWindow.end} of {preview.totalRows}</span><span>Generated {new Date(preview.generatedAt).toLocaleString()}</span></div><div className="rounded-2xl bg-slate-50 px-4 py-3 text-sm text-slate-600">Preview reflects the last successful run for the current filters.</div></div>{appliedFilters.length > 0 && <div className="rounded-2xl border border-gray-200 bg-white p-4"><div className="mb-3 flex items-center gap-2 text-sm font-medium text-gray-700"><Filter className="h-4 w-4 text-blue-600" />Applied filters</div><div className="flex flex-wrap gap-2">{appliedFilters.map(([key, value]) => <span key={key} className="rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-700">{key}: {value}</span>)}</div></div>}{preview.validationMessages.length > 0 && <div className="space-y-1 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">{preview.validationMessages.map((message) => <p key={message}>{message}</p>)}</div>}<div className="overflow-auto rounded-2xl border border-gray-200"><table className="min-w-full text-sm"><thead className="bg-gray-50 text-left text-gray-500"><tr>{preview.columns.map((column) => <th key={column} className="px-3 py-3 font-medium"><button type="button" onClick={() => { if (sortColumn === column) setSortDirection((current) => current === 'asc' ? 'desc' : 'asc'); else { setSortColumn(column); setSortDirection('asc'); } }} className="inline-flex items-center gap-2 text-left text-inherit hover:text-slate-900">{column}{sortColumn === column && <span className="text-xs text-blue-600">{sortDirection === 'asc' ? '?' : '?'}</span>}</button></th>)}</tr></thead><tbody>{sortedRows.map((row, index) => <tr key={index} className="border-t border-gray-100">{preview.columns.map((column) => <td key={column} className="px-3 py-3 text-gray-700">{formatValue(row[column])}</td>)}</tr>)}</tbody></table></div><div className="flex flex-col gap-3 rounded-2xl border border-gray-200 px-4 py-3 text-sm text-gray-600 sm:flex-row sm:items-center sm:justify-between"><div className="flex flex-wrap items-center gap-3"><span>Page {preview.page} of {previewTotalPages}</span>{sortColumn && <span className="rounded-full bg-slate-100 px-2.5 py-1 text-xs text-slate-700">Sorted by {sortColumn} ({sortDirection})</span>}</div><div className="flex items-center gap-2"><button type="button" onClick={() => executePreview(Math.max(1, preview.page - 1), previewPageSize)} disabled={running || preview.page <= 1} className="inline-flex items-center gap-2 rounded-xl border border-gray-200 px-3 py-2 disabled:opacity-50"><ChevronLeft className="h-4 w-4" />Previous</button><button type="button" onClick={() => executePreview(Math.min(previewTotalPages, preview.page + 1), previewPageSize)} disabled={running || preview.page >= previewTotalPages} className="inline-flex items-center gap-2 rounded-xl border border-gray-200 px-3 py-2 disabled:opacity-50">Next<ChevronRight className="h-4 w-4" /></button></div></div></div> : <div className="rounded-2xl border border-dashed border-gray-300 bg-gray-50 p-10 text-center text-sm text-gray-500">Run a report to load the preview, validation messages, and export-ready dataset.</div>}</div>

          <div className="rounded-3xl border border-gray-200 bg-white p-5 shadow-sm"><div className="mb-4 flex items-center gap-2"><History className="h-5 w-5 text-blue-600" /><h3 className="text-lg font-semibold text-gray-900">Recent History</h3></div><div className="space-y-3">{history.length === 0 && <div className="rounded-2xl border border-dashed border-gray-300 bg-gray-50 p-6 text-sm text-gray-500">No report executions yet. Preview or export a report to build an audit trail here.</div>}{history.map((item) => <div key={`${item.runId}-${item.reportCode}`} className="rounded-2xl border border-gray-100 p-3 text-sm"><div className="flex items-center justify-between gap-3"><div><p className="font-semibold text-gray-900">{item.reportName}</p><p className="text-gray-500">{item.reportCode} â€¢ {item.format}</p></div><span className={`rounded-full px-3 py-1 text-xs font-semibold ${item.status === 'Completed' ? 'bg-emerald-100 text-emerald-700' : item.status === 'Failed' ? 'bg-rose-100 text-rose-700' : 'bg-amber-100 text-amber-700'}`}>{item.status}</span></div><div className="mt-2 flex flex-wrap gap-4 text-gray-500"><span>{item.rowCount} rows</span><span>{new Date(item.startedAt).toLocaleString()}</span><span>{item.generatedBy}</span><button type="button" onClick={() => openHistoryReport(item.reportCode)} className="font-medium text-blue-700 hover:text-blue-800">Open report</button></div></div>)}</div></div>
        </div>
      </div>
    </div>
  );
}







