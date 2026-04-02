# Pre-Import Exceptions And Review Notes

## Purpose

This file captures the notable assumptions, exclusions, and review items from the legacy CBS mapping pass before importing the prepared data into BankInsight and CoreBanker.

## Prepared Import Files

Directly importable into the current migration API:

- `customers.csv`
- `products.csv`
- `accounts.csv`
- `loans.csv`
- `gl_accounts_template.csv`

Ancillary files that are not directly imported by the current migration API:

- `customer_account_enrichment.csv`
- `loan_balance_enrichment.csv`
- `cheque_inventory.csv`

## Confirmed Data Handling

- Two malformed legacy account rows were excluded because the source account number resolved to `.00` instead of a valid account identifier.
- Product identifiers were generated deterministically from legacy product names to preserve consistent references across repeated prep runs.
- Legacy branch references were normalized to the currently seeded BankInsight branches:
  - `01`, `Makola`, `Avenor`, `Head Office` -> `BR001`
  - `02`, `Kumasi` -> `BR002`
  - unknown values defaulted to `BR001`

## Items Requiring Manual Review

### GL Accounts

- No chart-of-accounts source workbook was included in the provided archives.
- `gl_accounts_template.csv` is only a template and must be manually populated before importing `gl_accounts`.

### Branch Crosswalk

- The source system appears to use more branch naming variations than the current target seed data.
- If the target environment uses a richer branch master than `BR001` and `BR002`, update the branch mapping in the prep tool before final import.

### Loan Status Semantics

- Loan status was normalized using available disbursement date, outstanding balance, and contract status clues.
- If the legacy CBS has stricter closed, written-off, restructured, or suspended classifications, review `loans.csv` before import.

### Product Catalog Semantics

- Products were generated from distinct source names rather than imported from a formal product master.
- Review `products.csv` if you need:
  - tighter type classification
  - normalized naming
  - curated interest methods
  - min/max amount and term rules

### Customer Completeness

- Customer email, digital address, occupation, and related KYC metadata are incomplete in many legacy rows.
- The main import files preserve what exists, while the ancillary enrichment files retain extra fields for post-import enrichment.

### Cheque Inventory

- `cheque_inventory.csv` was prepared from the cheque-number workbook.
- The current migration API does not import cheque-book or cheque-leaf inventory directly.
- Use this file later for cheque-book seeding or a dedicated cheque inventory import utility.

## Recommended Review Order

1. Review `products.csv`
2. Review `loans.csv` for status and collateral normalization
3. Populate `gl_accounts_template.csv` if GL migration is in scope
4. Review branch assumptions if the target branch master is broader than the default seed
5. Import `customers`, `products`, `accounts`, and `loans`
6. Use ancillary files for follow-up enrichment and cheque inventory setup
