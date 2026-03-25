import React, { useEffect, useMemo, useState } from 'react';
import {
  AlertCircle,
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Loader2,
  RefreshCw,
  ShieldCheck,
  UploadCloud,
} from 'lucide-react';
import { ApiError } from '../services/httpClient';
import { migrationService, MigrationDatasetInfo, MigrationImportResult } from '../services/migrationService';

interface FilePreview {
  headers: string[];
  rows: string[][];
}

const parseCsvPreview = async (file: File): Promise<FilePreview> => {
  const text = await file.text();
  const lines = text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

  if (lines.length === 0) {
    return { headers: [], rows: [] };
  }

  const parseLine = (line: string) => line.split(',').map((cell) => cell.trim().replace(/^"|"$/g, ''));
  return {
    headers: parseLine(lines[0]),
    rows: lines.slice(1, 6).map(parseLine),
  };
};

const downloadTemplate = (dataset: MigrationDatasetInfo) => {
  const blob = new Blob([`${dataset.requiredColumns.join(',')}\n`], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = `${dataset.key}_template.csv`;
  anchor.click();
  URL.revokeObjectURL(url);
};

export default function MigrationHub() {
  const [datasets, setDatasets] = useState<MigrationDatasetInfo[]>([]);
  const [selectedDatasetKey, setSelectedDatasetKey] = useState('');
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<FilePreview>({ headers: [], rows: [] });
  const [result, setResult] = useState<MigrationImportResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [infoMessage, setInfoMessage] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);

  const selectedDataset = useMemo(
    () => datasets.find((entry) => entry.key === selectedDatasetKey) ?? null,
    [datasets, selectedDatasetKey],
  );

  const loadDatasets = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await migrationService.getDatasets();
      setDatasets(response);
      setSelectedDatasetKey((current) => current || response[0]?.key || '');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unable to load migration datasets.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadDatasets();
  }, []);

  useEffect(() => {
    if (!selectedFile) {
      setPreview({ headers: [], rows: [] });
      return;
    }

    parseCsvPreview(selectedFile)
      .then(setPreview)
      .catch(() => setPreview({ headers: [], rows: [] }));
  }, [selectedFile]);

  const handleImport = async () => {
    if (!selectedDataset || !selectedFile) {
      setError('Select a dataset and CSV file before starting the import.');
      return;
    }

    setIsUploading(true);
    setError(null);
    setInfoMessage(null);

    try {
      const response = await migrationService.importDataset(selectedDataset.key, selectedFile);
      setResult(response);
      setInfoMessage(
        response.failed > 0
          ? `Import completed with ${response.failed} failed row(s). Review the error log before the next run.`
          : 'Import completed successfully. Retain the source file and result summary for audit support.',
      );
    } catch (err) {
      setError(
        err instanceof ApiError || err instanceof Error
          ? err.message
          : 'Import failed. Please review the file and try again.',
      );
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <div className="min-h-full space-y-6 p-4 sm:p-6">
      <div className="dashboard-sheen rounded-2xl border border-slate-200 p-6 text-slate-900 shadow-soft">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <p className="mb-1 text-[11px] font-accent uppercase tracking-[0.24em] text-slate-500">Data Migration</p>
            <h1 className="text-3xl font-heading font-semibold tracking-[-0.04em] text-slate-900">Migration Workbench</h1>
            <p className="mt-2 max-w-3xl text-sm text-slate-600">
              Import legacy customer, product, account, loan, and chart-of-accounts data through governed CSV jobs.
              Pick the dataset, download the matching template, preview the file, and review the API result before the next batch.
            </p>
          </div>
          <button
            type="button"
            onClick={loadDatasets}
            className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-50"
          >
            <RefreshCw size={16} className={isLoading ? 'animate-spin' : ''} />
            Refresh datasets
          </button>
        </div>
      </div>

      {error && (
        <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 p-4 text-sm text-red-800">
          <AlertCircle className="mt-0.5 h-5 w-5 flex-shrink-0" />
          <p>{error}</p>
        </div>
      )}

      {infoMessage && (
        <div className="flex items-start gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 p-4 text-sm text-emerald-800">
          <CheckCircle2 className="mt-0.5 h-5 w-5 flex-shrink-0" />
          <p>{infoMessage}</p>
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <div className="glass-card rounded-[28px] border border-white/70 p-6">
          <div className="mb-4 flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-brand-50 text-brand-600">
              <FileSpreadsheet className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-heading font-bold text-slate-900">Dataset Catalog</h2>
              <p className="text-sm text-slate-500">Choose the import pack and download the aligned template.</p>
            </div>
          </div>

          {isLoading ? (
            <div className="flex min-h-[220px] items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-brand-600" />
            </div>
          ) : (
            <div className="space-y-3">
              {datasets.map((dataset) => {
                const selected = dataset.key === selectedDatasetKey;
                return (
                  <button
                    key={dataset.key}
                    type="button"
                    onClick={() => {
                      setSelectedDatasetKey(dataset.key);
                      setResult(null);
                      setInfoMessage(null);
                    }}
                    className={`w-full rounded-[24px] border p-4 text-left transition ${
                      selected
                        ? 'border-brand-300 bg-brand-50/70 shadow-sm'
                        : 'border-slate-200 bg-white hover:border-brand-200 hover:bg-slate-50'
                    }`}
                  >
                    <div className="flex items-start justify-between gap-4">
                      <div>
                        <h3 className="text-base font-semibold text-slate-900">{dataset.name}</h3>
                        <p className="mt-1 text-sm text-slate-600">{dataset.description}</p>
                      </div>
                      {selected && (
                        <span className="rounded-full bg-brand-600 px-2.5 py-1 text-xs font-semibold text-white">
                          Selected
                        </span>
                      )}
                    </div>
                    <div className="mt-4 flex flex-wrap gap-2">
                      {dataset.requiredColumns.slice(0, 6).map((column) => (
                        <span key={column} className="rounded-full bg-white/85 px-2.5 py-1 text-xs font-medium text-slate-600 ring-1 ring-slate-200">
                          {column}
                        </span>
                      ))}
                      {dataset.requiredColumns.length > 6 && (
                        <span className="rounded-full bg-white/85 px-2.5 py-1 text-xs font-medium text-slate-500 ring-1 ring-slate-200">
                          +{dataset.requiredColumns.length - 6} more
                        </span>
                      )}
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        <div className="glass-card rounded-[28px] border border-white/70 p-6">
          <div className="mb-4 flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-emerald-50 text-emerald-600">
              <ShieldCheck className="h-5 w-5" />
            </div>
            <div>
              <h2 className="text-lg font-heading font-bold text-slate-900">Import Controls</h2>
              <p className="text-sm text-slate-500">Validate the file shape before the import is posted.</p>
            </div>
          </div>

          {selectedDataset ? (
            <div className="space-y-4">
              <div className="rounded-[22px] border border-slate-200 bg-slate-50 p-4">
                <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                  <div>
                    <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Template</p>
                    <p className="mt-1 text-sm font-semibold text-slate-900">{selectedDataset.templatePath}</p>
                  </div>
                  <button
                    type="button"
                    onClick={() => downloadTemplate(selectedDataset)}
                    className="inline-flex items-center gap-2 rounded-full border border-slate-300 bg-white px-4 py-2 text-sm font-medium text-slate-700 transition hover:bg-slate-100"
                  >
                    <Download size={16} />
                    Download CSV template
                  </button>
                </div>
              </div>

              <label className="block rounded-[24px] border-2 border-dashed border-slate-300 bg-white p-6 text-center transition hover:border-brand-300 hover:bg-brand-50/20">
                <input
                  type="file"
                  accept=".csv,text/csv"
                  className="hidden"
                  onChange={(event) => {
                    const file = event.target.files?.[0] ?? null;
                    setSelectedFile(file);
                    setResult(null);
                    setInfoMessage(null);
                    setError(null);
                  }}
                />
                <UploadCloud className="mx-auto h-8 w-8 text-brand-600" />
                <p className="mt-3 text-sm font-semibold text-slate-900">
                  {selectedFile ? selectedFile.name : 'Select a CSV file to import'}
                </p>
                <p className="mt-1 text-xs text-slate-500">Only CSV files are accepted by the live import endpoint.</p>
              </label>

              <div className="rounded-[22px] border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900">
                <p className="font-semibold">Pre-import checklist</p>
                <ul className="mt-2 space-y-1 text-amber-800">
                  <li>Keep header names unchanged from the dataset template.</li>
                  <li>Import in order: customers, products, accounts, loans, then GL accounts.</li>
                  <li>Run imports only with an admin account and retain the source file for audit review.</li>
                </ul>
              </div>

              <button
                type="button"
                onClick={handleImport}
                disabled={!selectedFile || isUploading}
                className="inline-flex w-full items-center justify-center gap-2 rounded-full bg-brand-600 px-5 py-3 text-sm font-semibold text-white transition hover:bg-brand-700 disabled:cursor-not-allowed disabled:bg-slate-300"
              >
                {isUploading ? <Loader2 className="h-4 w-4 animate-spin" /> : <UploadCloud className="h-4 w-4" />}
                {isUploading ? 'Importing dataset...' : `Import ${selectedDataset.name}`}
              </button>
            </div>
          ) : (
            <div className="rounded-[22px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">
              No migration datasets are available yet.
            </div>
          )}
        </div>
      </div>

      <div className="grid gap-6 xl:grid-cols-[0.95fr_1.05fr]">
        <div className="glass-card rounded-[28px] border border-white/70 p-6">
          <h2 className="text-lg font-heading font-bold text-slate-900">File Preview</h2>
          <p className="mt-1 text-sm text-slate-500">First rows from the selected CSV so operators can verify headers before posting.</p>

          {!selectedFile ? (
            <div className="mt-4 rounded-[22px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">
              Choose a CSV file to preview its headers and first records.
            </div>
          ) : (
            <div className="mt-4 space-y-4">
              <div className="rounded-[22px] border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Detected headers</p>
                <div className="mt-3 flex flex-wrap gap-2">
                  {preview.headers.length > 0 ? preview.headers.map((header) => (
                    <span key={header} className="rounded-full bg-white px-2.5 py-1 text-xs font-medium text-slate-700 ring-1 ring-slate-200">
                      {header}
                    </span>
                  )) : (
                    <span className="text-sm text-slate-500">Unable to read preview headers from this file.</span>
                  )}
                </div>
              </div>

              {preview.rows.length > 0 && (
                <div className="overflow-hidden rounded-[22px] border border-slate-200">
                  <div className="overflow-x-auto">
                    <table className="min-w-full divide-y divide-slate-200 text-sm">
                      <thead className="bg-slate-50">
                        <tr>
                          {preview.headers.map((header) => (
                            <th key={header} className="px-4 py-3 text-left font-semibold text-slate-700">
                              {header}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody className="divide-y divide-slate-100 bg-white">
                        {preview.rows.map((row, rowIndex) => (
                          <tr key={`${rowIndex}-${row.join('|')}`}>
                            {preview.headers.map((header, cellIndex) => (
                              <td key={`${header}-${cellIndex}`} className="px-4 py-3 text-slate-600">
                                {row[cellIndex] || '—'}
                              </td>
                            ))}
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>

        <div className="glass-card rounded-[28px] border border-white/70 p-6">
          <h2 className="text-lg font-heading font-bold text-slate-900">Import Results</h2>
          <p className="mt-1 text-sm text-slate-500">Operational summary and row-level issues returned by the API.</p>

          {!result ? (
            <div className="mt-4 rounded-[22px] border border-slate-200 bg-slate-50 p-6 text-sm text-slate-500">
              No import has been run in this session yet.
            </div>
          ) : (
            <div className="mt-4 space-y-4">
              <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
                {[
                  { label: 'Rows', value: result.totalRows, tone: 'border-slate-200 bg-slate-50 text-slate-700' },
                  { label: 'Imported', value: result.imported, tone: result.imported > 0 ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-slate-50 text-slate-600' },
                  { label: 'Updated', value: result.updated, tone: result.updated > 0 ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-slate-200 bg-slate-50 text-slate-600' },
                  { label: 'Failed', value: result.failed, tone: result.failed > 0 ? 'border-amber-200 bg-amber-50 text-amber-700' : 'border-slate-200 bg-slate-50 text-slate-600' },
                ].map((metric) => (
                  <div key={metric.label} className={`rounded-[22px] border p-4 ${metric.tone}`}>
                    <p className="text-xs uppercase tracking-[0.18em]">{metric.label}</p>
                    <p className="mt-2 text-2xl font-bold">{metric.value}</p>
                  </div>
                ))}
              </div>

              <div className="rounded-[22px] border border-slate-200 bg-slate-50 p-4">
                <p className="text-xs uppercase tracking-[0.18em] text-slate-500">Dataset</p>
                <p className="mt-1 text-sm font-semibold text-slate-900">{result.dataset}</p>
              </div>

              <div className="rounded-[22px] border border-slate-200 bg-white">
                <div className="border-b border-slate-200 px-4 py-3">
                  <p className="text-sm font-semibold text-slate-900">Error log</p>
                </div>
                <div className="max-h-72 overflow-y-auto px-4 py-3">
                  {result.errors.length === 0 ? (
                    <p className="text-sm text-emerald-700">No row-level errors were reported for this import.</p>
                  ) : (
                    <div className="space-y-2">
                      {result.errors.map((entry) => (
                        <div key={entry} className="rounded-xl border border-red-100 bg-red-50 px-3 py-2 text-sm text-red-800">
                          {entry}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
