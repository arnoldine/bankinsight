# Migration Execution Notes

## Available Utilities

- Main dataset import:
  - [import-bankinsight-migration.ps1](C:\Backup old\dev\bankinsight\migration_work\import-bankinsight-migration.ps1)
- Cheque inventory seeding:
  - [import-cheque-inventory.ps1](C:\Backup old\dev\bankinsight\migration_work\import-cheque-inventory.ps1)
- Post-import verification:
  - [verify-bankinsight-import.ps1](C:\Backup old\dev\bankinsight\migration_work\verify-bankinsight-import.ps1)
- Full orchestrated run:
  - [run-full-bankinsight-migration.ps1](C:\Backup old\dev\bankinsight\migration_work\run-full-bankinsight-migration.ps1)

## Recommended Execution Order

1. Review:
   - [MIGRATION_MAPPING_SUMMARY.md](C:\Backup old\dev\bankinsight\migration_work\prepared\MIGRATION_MAPPING_SUMMARY.md)
   - [PRE_IMPORT_EXCEPTIONS.md](C:\Backup old\dev\bankinsight\migration_work\prepared\PRE_IMPORT_EXCEPTIONS.md)
2. Populate:
   - [gl_accounts_template.csv](C:\Backup old\dev\bankinsight\migration_work\prepared\gl_accounts_template.csv) if GL migration is in scope
3. Import primary datasets with:
   - [import-bankinsight-migration.ps1](C:\Backup old\dev\bankinsight\migration_work\import-bankinsight-migration.ps1)
4. Verify counts with:
   - [verify-bankinsight-import.ps1](C:\Backup old\dev\bankinsight\migration_work\verify-bankinsight-import.ps1)
5. Seed cheque books and leaves with:
   - [import-cheque-inventory.ps1](C:\Backup old\dev\bankinsight\migration_work\import-cheque-inventory.ps1)
6. Review used-leaf reconciliation output:
   - `cheque-inventory-used-leaves-review.csv`

Or run the whole sequence with:

- [run-full-bankinsight-migration.ps1](C:\Backup old\dev\bankinsight\migration_work\run-full-bankinsight-migration.ps1)

## Important Limitation

The current BankInsight cheque-book API now supports:

- stock intake
- issuance to account
- cancellation of unused leaves
- historical marking of issued leaves as already used

The cheque import utility will now:

- seed stock
- issue books to account
- mark historically used leaves through the new API path where possible

Any remaining used leaves that fail reconciliation are written to:

- `cheque-inventory-used-leaves-review.csv`
