const form = document.getElementById('migrationForm');
const apiBaseInput = document.getElementById('apiBase');
const datasetInput = document.getElementById('dataset');
const fileInput = document.getElementById('csvFile');
const resultPanel = document.getElementById('resultPanel');
const resultText = document.getElementById('resultText');
const classifierSummary = document.getElementById('classifierSummary');
const classifierBody = document.getElementById('classifierBody');
const precheckPanel = document.getElementById('precheckPanel');
const precheckSummary = document.getElementById('precheckSummary');
const precheckIssues = document.getElementById('precheckIssues');

const classifierByDataset = {
  customers: './templates/customers_template_classifier.csv',
  products: './templates/products_template_classifier.csv',
  accounts: './templates/accounts_template_classifier.csv',
  loans: './templates/loans_template_classifier.csv',
  'gl-accounts': './templates/gl_accounts_template_classifier.csv',
};

let currentRules = [];

function splitCsvLine(line) {
  const fields = [];
  let current = '';
  let inQuotes = false;

  for (let i = 0; i < line.length; i += 1) {
    const char = line[i];
    const next = line[i + 1];

    if (char === '"') {
      if (inQuotes && next === '"') {
        current += '"';
        i += 1;
      } else {
        inQuotes = !inQuotes;
      }
      continue;
    }

    if (char === ',' && !inQuotes) {
      fields.push(current);
      current = '';
      continue;
    }

    current += char;
  }

  fields.push(current);
  return fields;
}

function parseCsvText(text) {
  const lines = text
    .replace(/\r/g, '')
    .split('\n')
    .filter((line) => line.trim().length > 0);

  if (lines.length === 0) {
    return { headers: [], rows: [] };
  }

  const headers = splitCsvLine(lines[0]).map((header) => header.trim());
  const rows = lines.slice(1).map((line) => {
    const values = splitCsvLine(line);
    const row = {};
    headers.forEach((header, index) => {
      row[header] = (values[index] || '').trim();
    });
    return row;
  });

  return { headers, rows };
}

function parseAllowedValues(ruleText) {
  if (!ruleText) {
    return [];
  }

  if (!ruleText.includes('|')) {
    return [];
  }

  return ruleText
    .split('|')
    .map((value) => value.trim())
    .filter((value) => value.length > 0);
}

function isIsoDate(value) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const date = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(date.getTime());
}

function isIsoDateTime(value) {
  const date = new Date(value);
  return !Number.isNaN(date.getTime());
}

function validateValue(rule, value) {
  if (!value) {
    return null;
  }

  const dataType = (rule.data_type || '').toLowerCase();

  if (dataType === 'decimal' && Number.isNaN(Number(value))) {
    return 'Expected decimal value.';
  }

  if (dataType === 'integer' && !/^[-+]?\d+$/.test(value)) {
    return 'Expected integer value.';
  }

  if (dataType === 'boolean') {
    const lower = value.toLowerCase();
    if (!['true', 'false', '1', '0'].includes(lower)) {
      return 'Expected boolean value (true/false/1/0).';
    }
  }

  if (dataType === 'date' && !isIsoDate(value)) {
    return 'Expected date in YYYY-MM-DD format.';
  }

  if (dataType === 'datetime' && !isIsoDateTime(value)) {
    return 'Expected valid ISO-8601 date-time.';
  }

  const allowed = parseAllowedValues(rule.allowed_values_or_rules || '');
  if (allowed.length > 0) {
    const normalizedAllowed = allowed.map((item) => item.toLowerCase());
    if (!normalizedAllowed.includes(value.toLowerCase())) {
      return `Expected one of: ${allowed.join(', ')}.`;
    }
  }

  return null;
}

function renderClassifierRules(rules) {
  classifierBody.innerHTML = '';

  if (!rules || rules.length === 0) {
    classifierSummary.textContent = 'No classifier rules available for the selected dataset.';
    return;
  }

  classifierSummary.textContent = `Loaded ${rules.length} field rules for ${datasetInput.value}.`;

  rules.forEach((rule) => {
    const row = document.createElement('tr');
    row.innerHTML = `
      <td><code>${rule.field_name || ''}</code></td>
      <td>${rule.data_type || ''}</td>
      <td>${rule.required || ''}</td>
      <td>${rule.allowed_values_or_rules || ''}</td>
      <td>${rule.format || ''}</td>
      <td>${rule.example || ''}</td>
    `;
    classifierBody.appendChild(row);
  });
}

async function loadClassifierRules(dataset) {
  const path = classifierByDataset[dataset];
  if (!path) {
    currentRules = [];
    renderClassifierRules(currentRules);
    return;
  }

  try {
    const response = await fetch(path);
    if (!response.ok) {
      throw new Error('Unable to load classifier file.');
    }

    const text = await response.text();
    const { rows } = parseCsvText(text);
    currentRules = rows;
    renderClassifierRules(currentRules);
  } catch (error) {
    currentRules = [];
    classifierSummary.textContent = `Classifier load failed: ${error.message}`;
    classifierBody.innerHTML = '';
  }
}

function showPrecheck(summaryText, issues) {
  precheckSummary.className = issues.length === 0 ? 'status-ok' : 'status-warn';
  precheckSummary.textContent = summaryText;
  precheckIssues.innerHTML = '';

  issues.slice(0, 30).forEach((issue) => {
    const li = document.createElement('li');
    li.textContent = issue;
    precheckIssues.appendChild(li);
  });

  if (issues.length > 30) {
    const li = document.createElement('li');
    li.textContent = `... ${issues.length - 30} more issue(s) not shown.`;
    precheckIssues.appendChild(li);
  }

  precheckPanel.hidden = false;
}

async function runPrecheck(file) {
  if (!file || currentRules.length === 0) {
    precheckPanel.hidden = true;
    return { issues: [] };
  }

  const text = await file.text();
  const { headers, rows } = parseCsvText(text);
  const issues = [];
  const rulesByField = new Map(currentRules.map((rule) => [rule.field_name, rule]));

  currentRules.forEach((rule) => {
    if (!headers.includes(rule.field_name)) {
      issues.push(`Missing expected column: ${rule.field_name}`);
    }
  });

  rows.forEach((row, rowIndex) => {
    currentRules.forEach((rule) => {
      const field = rule.field_name;
      const value = (row[field] || '').trim();
      const required = String(rule.required || '').toUpperCase() === 'YES';

      if (required && !value) {
        issues.push(`Row ${rowIndex + 2}: ${field} is required.`);
        return;
      }

      const validationError = validateValue(rule, value);
      if (validationError) {
        issues.push(`Row ${rowIndex + 2}: ${field} -> ${validationError}`);
      }
    });

    Object.keys(row).forEach((header) => {
      if (!rulesByField.has(header)) {
        issues.push(`Row ${rowIndex + 2}: unexpected column '${header}'.`);
      }
    });
  });

  const summary = issues.length === 0
    ? `Precheck passed: ${rows.length} data row(s) validated with no classifier issues.`
    : `Precheck found ${issues.length} issue(s) across ${rows.length} data row(s).`;

  showPrecheck(summary, issues);
  return { issues };
}

function renderResult(payload) {
  const lines = [
    `Dataset: ${payload.dataset}`,
    `Total Rows: ${payload.totalRows}`,
    `Imported: ${payload.imported}`,
    `Updated: ${payload.updated}`,
    `Failed: ${payload.failed}`,
  ];

  if (Array.isArray(payload.errors) && payload.errors.length > 0) {
    lines.push('', 'Errors:');
    payload.errors.forEach((error) => lines.push(`- ${error}`));
  }

  resultText.textContent = lines.join('\n');
  resultPanel.hidden = false;
}

function renderError(message) {
  resultText.textContent = message;
  resultText.className = 'status-error';
  resultPanel.hidden = false;
}

datasetInput.addEventListener('change', async () => {
  await loadClassifierRules(datasetInput.value);

  const file = fileInput.files?.[0];
  if (file) {
    await runPrecheck(file);
  }
});

fileInput.addEventListener('change', async () => {
  const file = fileInput.files?.[0];
  if (!file) {
    precheckPanel.hidden = true;
    return;
  }

  await runPrecheck(file);
});

form.addEventListener('submit', async (event) => {
  event.preventDefault();

  const file = fileInput.files?.[0];
  if (!file) {
    renderError('Please select a CSV file before running import.');
    return;
  }

  const dataset = datasetInput.value;
  const apiBase = apiBaseInput.value.trim().replace(/\/$/, '');
  const formData = new FormData();
  formData.append('file', file);

  const precheck = await runPrecheck(file);
  if (precheck.issues.length > 0) {
    renderError('CSV precheck found issues. Resolve them before import.');
    return;
  }

  resultText.className = '';
  resultText.textContent = 'Import in progress...';
  resultPanel.hidden = false;

  try {
    const response = await fetch(`${apiBase}/migration/import/${dataset}`, {
      method: 'POST',
      body: formData,
    });

    const payload = await response.json();
    if (!response.ok) {
      renderError(payload.message || 'Migration request failed.');
      return;
    }

    renderResult(payload);
  } catch (error) {
    renderError(`Migration request failed: ${error.message}`);
  }
});

loadClassifierRules(datasetInput.value);
