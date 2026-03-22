# BankInsight Developer Handoff Guide
## Quick Reference for Next Phase Implementation

---

## 🎯 Quick Start (You Are Here)

### Current Status
- **Phase 1:** ✅ 100% Complete (10/10 tests passing)
- **Phase 2:** 🟡 43% Complete (3/7 tests passing - 4 endpoints need fixes)
- **Phase 3:** 🟠 60% Complete (Framework ready, missing 2 controllers)
- **Phase 4:** ✅ 100% Complete (Frontend integrated)

### Services Running
```powershell
# Backend: http://localhost:5176
dotnet run --cwd BankInsight.API

# Database: PostgreSQL on localhost:5432
docker ps  # Should show bankinsight-postgres

# Frontend (optional): http://localhost:3000
npm run dev
```

---

## 🔧 Priority Bug Fixes (Next 4-5 Hours)

### Issue 1: Treasury Position 400 Bad Request
**File:** `BankInsight.API/Controllers/TreasuryPositionController.cs`  
**Problem:** POST /api/treasuryposition returns 400  
**Root Cause:** Likely EF Core validation or SaveChanges exception

**Debug Checklist:**
```csharp
// 1. Add to TreasuryPositionService.CreatePositionAsync():
_logger.LogError("Creating position: {Json}", JsonSerializer.Serialize(new { 
    request.PositionDate, request.Currency, request.OpeningBalance 
}));

// 2. Wrap SaveChangesAsync in try-catch:
try {
    await _context.SaveChangesAsync();
} catch (DbUpdateException ex) {
    _logger.LogError(ex, "DB Error: {Message}", ex.InnerException?.Message);
    throw;
}

// 3. Check TreasuryPosition entity for required fields:
// - Are all [Required] fields being set?
// - Is ReconciledBy (FK) causing issues?
```

**Test After Fix:**
```powershell
$position = @{ positionDate = "2026-03-03"; currency = "GHS"; openingBalance = 1000000 }
Invoke-RestMethod -Uri "http://localhost:5176/api/treasuryposition" `
  -Method Post -Headers @{ Authorization = "Bearer $token" } `
  -Body ($position | ConvertTo-Json)
```

### Issue 2: FX Trading 500 Internal Server Error
**File:** `BankInsight.API/Services/FxTradingService.cs`  
**Problem:** POST /api/fxtrading returns 500  
**Root Cause:** Unhandled exception in service

**Debug Checklist:**
```csharp
// 1. Check FxTradingService.CreateTradeAsync()
// Look for:
// - GenerateDealNumber() logic
// - Any LINQ that might fail
// - SaveChangesAsync() exceptions

// 2. Add debugging:
_logger.LogInformation("Generate deal number: Current = {Time}", DateTime.Now.ToString("yyyyMMdd"));
var dealNumber = GenerateDealNumber();
_logger.LogInformation("Generated deal: {DealNumber}", dealNumber);
```

**Test After Fix:**
```powershell
$trade = @{ 
    tradeType = "Spot"; direction = "Buy"; 
    baseCurrency = "USD"; baseAmount = 100000;
    counterCurrency = "GHS"; exchangeRate = 11.50
}
Invoke-RestMethod -Uri "http://localhost:5176/api/fxtrading" `
  -Method Post -Body ($trade | ConvertTo-Json)
```

### Issue 3: Investment 400 Validation Error
**File:** `BankInsight.API/DTOs/TreasuryDTOs.cs`  
**Problem:** POST /api/investment returns 400 (model validation)  
**Root Cause:** Missing required fields in CreateInvestmentRequest DTO

**Debug Checklist:**
```csharp
// 1. Review CreateInvestmentRequest DTO:
// What fields are marked [Required]?
// Are all of them being sent in the test request?

// 2. Check Investment entity for required fields:
// - investmentNumber (auto-generated? or required in DTO?)
// - placement_date required?
// - maturity_date required?

// 3. Test with complete request:
$investment = @{
    investmentType = "T-Bill"
    instrument = "91-Day T-Bill"
    currency = "GHS"
    principalAmount = 500000
    interestRate = 18.5
    discountRate = 0
    placementDate = "2026-03-03"
    maturityDate = "2026-06-03"
    tenorDays = 91
}
```

### Issue 4: Risk Analytics 400 Parameter Error
**File:** `BankInsight.API/Controllers/RiskAnalyticsController.cs`  
**Problem:** POST /api/riskanalytics/var returns 400  
**Root Cause:** Endpoint expects query params, not POST body

**Quick Fix:**
```csharp
// Current (Wrong):
[HttpPost("var")]
public async Task<ActionResult<RiskMetricDto>> CalculateVaR(
    [FromBody] RiskMetricRequest request)

// Fixed:
[HttpGet("var")]
public async Task<ActionResult<RiskMetricDto>> CalculateVaR(
    [FromQuery] DateTime metricDate,
    [FromQuery] string currency,
    [FromQuery] int conf idenceLevel = 95,
    [FromQuery] int timeHorizonDays = 1)
```

**Test After Fix:**
```powershell
Invoke-RestMethod  -Uri "http://localhost:5176/api/riskanalytics/var?metricDate=2026-03-03&currency=GHS&confidenceLevel=95" `
  -Method Get -Headers $headers
```

---

## 📋 Key Files Quick Reference

### API Controllers (Location: `BankInsight.API/Controllers/`)
| File | Status | Fix Needed |
|------|--------|-----------|
| AuthController.cs | ✅ Working | No |
| AccountController.cs | ✅ Working | No |
| TransactionController.cs | ✅ Working | No |
| FxRateController.cs | ✅ Working | No |
| TreasuryPositionController.cs | 🔴 Broken | Yes - Add FK handling |
| FxTradingController.cs | 🔴 Broken | Yes - Check service |
| InvestmentController.cs | 🔴 Broken | Yes - Check DTO |
| RiskAnalyticsController.cs | 🔴 Broken | Yes - Change to GET |
| ReportingController.cs | ✅ Ready | No |
| RegulatoryReportsController.cs | 🟠 Stub | Needs implementation |
| FinancialReportsController.cs | 🟠 Stub | Needs implementation |

### Services (Location: `BankInsight.API/Services/`)
| Service | Lines | Status |
|---------|-------|--------|
| FxRateService.cs | 281 | ✅ Working |
| TreasuryPositionService.cs | 253 | 🔴 Debugging |
| FxTradingService.cs | 319 | 🔴 Debugging |
| InvestmentService.cs | ~400 | 🔴 Debugging |
| RiskAnalyticsService.cs | ~371 | 🔴 Parameter mismatch |
| ReportingService.cs | ~300 | ✅ Ready |
| RegulatoryReportService.cs | ~500 | 🟠 Needs logic |
| FinancialReportService.cs | ~400 | 🟠 Needs logic |

### DTOs (Location: `BankInsight.API/DTOs/TreasuryDTOs.cs`)
```csharp
// Check these for required fields:
public record CreateTreasuryPositionRequest(...)
public record CreateFxTradeRequest(...)
public record CreateInvestmentRequest(...)
public record RiskMetricRequest(...)
```

### Entities (Location: `BankInsight.API/Entities/`)
| Entity | Table | Status |
|--------|-------|--------|
| FxRate | fx_rates | ✅ Schema final |
| TreasuryPosition | treasury_positions | ✅ Schema final |
| FxTrade | fx_trades | ✅ Schema final |
| Investment | investments | ✅ Schema final |
| RiskMetric | risk_metrics | ✅ Schema final |

### Test Scripts (Location: `scripts/`)
| Script | Tests | Pass Rate | Use |
|--------|-------|-----------|-----|
| smoke-test.ps1 | 10 | 100% ✅ | Phase 1 validation |
| phase2-treasury-test.ps1 | 7 | 42.8% | Phase 2 validation |
| (TODO) phase3-reporting-test.ps1 | ~15 | TBD | Phase 3 validation |
| (TODO) phase1-security-test.ps1 | ~8 | TBD | Security features |

---

## 🚀 Immediate Action Items

### Step 1: Enable Debug Logging (15 min)
```csharp
// In Program.cs, before building:
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.SetMinimumLevel(LogLevel.Debug);
});

// In services, add:
_logger.LogDebug("Creating {Entity}: {@Data}", 
    nameof(TreasuryPosition), request);
```

### Step 2: Run Phase 2 Tests (5 min)
```powershell
cd c:\Backup old\dev\bankinsight
.\scripts\phase2-treasury-test.ps1
# Observe which tests fail and capture error messages
```

### Step 3: Fix Each Endpoint Sequentially
- Start with Issue 4 (Risk Analytics) - simplest fix
- Then Issue 3 (Investment) - review DTO
- Then Issue 1 (Treasury Position) - likely FK constraint
- Finally Issue 2 (FX Trading) - debug service logic

### Step 4: Re-run Tests After Each Fix
```powershell
.\scripts\phase2-treasury-test.ps1
# Target: 12/12 tests passing
```

---

## 💾 Database Queries (Helpful for Debugging)

```sql
-- Check if treasury tables exist:
SELECT table_name FROM information_schema.tables 
WHERE table_schema = 'public' AND table_name LIKE '%treasury%';

-- Check fx_rates data:
SELECT * FROM fx_rates ORDER BY created_at DESC LIMIT 5;

-- Check for recent transactions:
SELECT * FROM audit_logs WHERE action = 'POST_TRANSACTION' 
ORDER BY created_at DESC LIMIT 5;

-- Verify staff records exist (for FK):
SELECT id, name FROM staff WHERE is_active = true LIMIT 5;
```

---

## 🎯 Phase 3 Implementation (After Phase 2 Fixed)

### RegulatoryReportsController - Template
```csharp
// BankInsight.API/Controllers/RegulatoryReportsController.cs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegulatoryReportsController : ControllerBase
{
    private readonly IRegulatoryReportService _reportService;
    
    [HttpPost("daily-position")]
    public async Task<ActionResult<DailyPositionReportDto>> GenerateDailyPositionReport(
        [FromBody] GenerateRegulatoryReportRequest request)
    {
        var report = await _reportService.GenerateDailyPositionReportAsync(
            request.ReportDate);
        return Ok(report);
    }
    
    // ... similar for monthly returns 1-3, prudential, large exposure
}
```

### Key Principle
Copy the working pattern from `FxRateController.cs`:
1. Inject service in constructor
2. Create controller action with DTO binding
3. Call service method (wrapped in try-catch)
4. Return appropriate HTTP status

---

## 📞 Common Commands

```powershell
# Rebuild everything
cd BankInsight.API
dotnet clean
dotnet build

# Run migrations (if schema changes)
dotnet ef database update

# View recent logs (filter by time)
dotnet run 2>&1 | Select-String "error|exception" -i

# Kill long-running processes
Get-Process | Where-Object {$_.Name -like "*dotnet*"} | Stop-Process -Force

# Test specific endpoint
$headers = @{ 
    Authorization = "Bearer $(JWT_TOKEN)"
    "Content-Type" = "application/json"
}
Invoke-RestMethod -Uri "http://localhost:5176/api/treasuryposition" `
  -Method Post -Headers $headers -Body @{...} | ConvertTo-Json
```

---

## 📚 Documentation Files

| File | Purpose | Read When |
|------|---------|-----------|
| SESSION-SUMMARY.md | Current session results + metrics | Starting this session |
| COMPREHENSIVE-IMPLEMENTATION-PLAN.md | Full 3-phase roadmap | Planning next phase |
| PHASE-2-TRANSITION.md | Phase 2 status details | Debugging Phase 2 |
| AUTOMATED-SMOKE-TEST.md | Phase 1 test guide | Validating Phase 1 |
| PHASE-2-IMPLEMENTATION.md | Phase 2 architecture | Understanding Treasury |
| PHASE-3-IMPLEMENTATION.md | Phase 3 architecture | Planning Reporting |
| PHASE-4-README.md | Frontend guide | Working on UI |

---

## ✅ Success Criteria (Next Session)

When you've completed Phase 2 fixes, you should have:
- [ ] `./scripts/phase2-treasury-test.ps1` showing **12/12 PASS** ✅
- [ ] All 4 failing endpoints returning correct HTTP status
- [ ] Detailed error messages in API responses
- [ ] GitHub commit with message: "Fix: Phase 2 treasury endpoints"

---

## 🎓 Learning Resources

**EF Core Debugging:**
- `DbUpdateException` often contains FK constraint violations
- Check `InnerException?.Message` for actual SQL error
- Use `.sql` logs: `DbCommand` in logs shows actual queries

**PowerShell REST Testing:**
- `Invoke-RestMethod` is simpler but shows fewer details
- `Invoke-WebRequest` gives more control over response handling
- Save Bearer token: `$token = (Invoke-RestMethod -Uri ... -Body @{...}).token`

**Common Errors:**
- `400 Bad Request` = Validation error in model binding
- `401 Unauthorized` = Missing or invalid JWT token
- `500 Internal Server Error` = Unhandled exception in service
- `409 Conflict` = Database constraint violation

---

**Good luck! You've got a solid foundation - just need to debug 4 endpoints to unlock Phase 2. 💪**

