# Trial Balance Migration Summary

- Source workbook: `C:\Users\awulu\Downloads\TB NEW.xlsx`
- Sheet: `Sheet1`
- Raw branch-level rows: `897`
- Aggregated GL accounts: `218`

Category mapping heuristic:
- `100-199` => `INCOME`
- `200-299` => `EXPENSE`
- `300-399` => `ASSET`
- `400-499` => `LIABILITY`
- `500-599` => `EQUITY`
- fallback uses description keywords before defaulting to `ASSET`