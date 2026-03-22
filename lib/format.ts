/**
 * BoG-compliant formatting for Ghana (en-GH).
 * Use for all currency displays.
 */

const ghanaCurrency = new Intl.NumberFormat('en-GH', {
  style: 'currency',
  currency: 'GHS',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatGhs(amount: number): string {
  return ghanaCurrency.format(amount);
}

export function formatGhsCompact(amount: number): string {
  return new Intl.NumberFormat('en-GH', {
    style: 'currency',
    currency: 'GHS',
    minimumFractionDigits: 0,
    maximumFractionDigits: 1,
    notation: amount >= 1_000_000 ? 'compact' : 'standard',
  }).format(amount);
}

export function formatNumber(value: number, decimals = 2): string {
  return new Intl.NumberFormat('en-GH', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);
}
