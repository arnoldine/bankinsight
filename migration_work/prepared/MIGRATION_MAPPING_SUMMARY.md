# Legacy CBS Migration Mapping Summary

## Prepared Files

Import-ready CSVs:

- `customers.csv`
- `products.csv`
- `accounts.csv`
- `loans.csv`
- `gl_accounts_template.csv`

Ancillary CSVs:

- `customer_account_enrichment.csv`
- `loan_balance_enrichment.csv`
- `cheque_inventory.csv`

## Record Counts

- Customers: 78645
- Products: 58
- Accounts: 139263
- Loans: 62366
- Customer enrichment rows: 139265
- Loan enrichment rows: 62366
- Cheque inventory rows: 79124

## Source-to-Target Mapping

### Customers

- Source workbook: `vwExp_bog_MAFI900_SingleCustomer_View-2026-03-31.xlsx`
- Source key: `Bank Specific CIN`
- Target dataset: `customers`
- Major mappings:
  - `Bank Specific CIN` -> `id`
  - `Customer Type` -> `type`
  - name parts / `Company Name` -> `name`
  - `Email` -> `email`
  - phone columns -> `phone`, `secondary_phone`
  - `DOB` -> `date_of_birth`
  - `ID Number` -> `ghana_card` when ID type is Ghana Card
  - `Country` -> `nationality`
  - supplementary `DEPOSIT` / `KYC` sheets enrich sector, marital status, digital address, and risk

### Accounts

- Source workbook: `vwExp_bog_MAFI900_SingleCustomer_View-2026-03-31.xlsx`
- Source key: `Account Number`
- Target dataset: `accounts`
- Major mappings:
  - `Account Number` -> `id`
  - `Bank Specific CIN` -> `customer_id`
  - `Product Name` -> `product_code` via generated product IDs
  - `Account Type` / `Product Name` -> normalized `type`
  - balance columns -> `balance`
  - `Status Of Account` -> `status`
  - supplementary `DEPOSIT.OPEN DATE` -> `created_at`

### Loans

- Source workbook: `vwExp_bog_MAFI100_ActiveCreditContracts-2026-03-31.xlsx`
- Supplementary workbook: `ADDITIONAL REQUIREMENTS_March2026.xlsx`, sheet `LOANS`
- Source key: `ContractNumber`
- Target dataset: `loans`
- Major mappings:
  - `ContractNumber` -> `id`
  - `Customer ID` -> `customer_id`
  - `Loan Product` -> `product_code` via generated product IDs
  - `Loan Amount Disbursed` / `Loan Amount Approved` -> `principal`
  - `Interest Rate (% P.A)` -> `rate`
  - payment count + frequency -> `term_months`
  - `Loan Disbursement Date` -> `disbursement_date`, `disbursed_at`
  - principal balance -> `outstanding_balance`
  - security columns -> `collateral_type`, `collateral_value`
  - supplementary `PaymentsContractNumber` -> `servicing_account_id`

### Products

- Generated from distinct account `Product Name` values and loan `Loan Product` values.
- Deposit-like products are tagged as `DEPOSIT`.
- Loan-like products are tagged as `LOAN`.

### GL Accounts

- No chart-of-accounts source file was present in the provided archives.
- `gl_accounts_template.csv` is included as a header-ready file for manual population.

## Important Assumptions

- Branch mapping is normalized to current seeded branches:
  - `01`, `Makola`, `Avenor`, `Head Office` -> `BR001`
  - `02`, `Kumasi` -> `BR002`
  - unknown values default to `BR001`
- The current migration API imports only `customers`, `products`, `accounts`, `loans`, and `gl_accounts`.
- Cheque numbers, detailed KYC thresholds, and extra deposit attributes are preserved in ancillary files for later use.
- Product IDs are generated deterministically from source names so the same source can be re-run consistently.

## Suggested Import Order

1. `customers.csv`
2. `products.csv`
3. `accounts.csv`
4. `loans.csv`
5. `gl_accounts_template.csv` after manual completion