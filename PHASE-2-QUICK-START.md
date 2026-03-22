# Phase 2 Treasury Management - Quick Start Guide

## 🚀 Getting Started

### 1. Start the API Server
```powershell
cd "c:\Backup old\dev\bankinsight\BankInsight.API"
dotnet run
# API runs on http://localhost:5176
```

### 2. Login to BankInsight
- **URL**: Frontend running locally (Vite dev server)
- **Email**: admin@bankinsight.local
- **Password**: password123

### 3. Navigate to Treasury Tab
In the App.tsx sidebar, click **"Treasury"** under the main navigation menu.

---

## 📊 Component Testing Scenarios

### Scenario 1: FX Rate Management
**Objective**: Test currency rate entry and conversion

**Steps**:
1. Click **"FX Rates"** tab in Treasury Hub
2. Click **"Add Rate"** button
3. Fill in:
   - Base Currency: GHS
   - Target Currency: EUR
   - Buy Rate: 13.00
   - Sell Rate: 13.10
4. Click **"Save Rate"**
5. In Currency Converter:
   - From: GHS
   - Amount: 10,000
   - To: EUR
   - Click **"Convert"**
6. **Expected**: Display conversion result using the rate you added

**Verification**:
- ✅ Rate appears in table
- ✅ Conversion calculates correctly
- ✅ Spread % displays (e.g., 0.77%)

---

### Scenario 2: Treasury Position Monitoring
**Objective**: Track daily cash position by currency

**Steps**:
1. Click **"Cash Position"** tab in Treasury Hub
2. Observe summary cards for GHS, USD, EUR, etc.
3. Click on **"GHS"** card
4. Review position details:
   - Opening balance
   - Closing balance
   - Deposits/Withdrawals
5. Scroll down to **"Position History"** table
6. **Expected**: Shows historical positions

**Verification**:
- ✅ Summary cards show current balance
- ✅ Position details card updates on selection
- ✅ History table shows multiple dates
- ✅ Utilization % calculated correctly

---

### Scenario 3: FX Trading Desk
**Objective**: Execute and approve FX trades

**Steps**:
1. Click **"FX Trading"** tab
2. Click **"New Trade"** button
3. Fill in trade form:
   - Trade Type: Spot
   - Direction: Buy
   - Buy Currency: USD
   - Amount: 50,000
   - Sell Currency: GHS
   - Amount: 575,000 (50,000 × 11.50)
   - Rate: 11.50
4. Click **"Execute Trade"**
5. **Expected**: Trade added to pending queue

**Verification**:
- ✅ Deal number generated (FX-YYYYMMDD-XXXXX)
- ✅ Trade appears in "Pending Approvals"
- ✅ "Approve" button available
- ✅ Trade moves to "Recent Trades" after approval

---

### Scenario 4: Investment Portfolio
**Objective**: Track investment instruments and maturity

**Steps**:
1. Click **"Investments"** tab
2. Observe portfolio summary:
   - Total investments count
   - Total principal amount
   - Accrued interest
   - Average yield
3. Scroll down to **"By Investment Type"** breakdown
4. Review **"Next 30 Days Maturity"** table
5. **Expected**: Shows investments maturing soon

**Verification**:
- ✅ Summary cards display aggregated values
- ✅ Type breakdown shows composition
- ✅ Maturity calendar sorts by date
- ✅ Days-to-maturity color-coded (red/yellow/green)

---

### Scenario 5: Risk Analytics Dashboard
**Objective**: Monitor risk metrics and thresholds

**Steps**:
1. Click **"Risk Analytics"** tab
2. Observe two main metrics:
   - **VaR** (Value at Risk): Shows current 1-day VaR in GHS
   - **LCR** (Liquidity Ratio): Shows % vs. 100% regulatory minimum
3. Review **"Currency Exposure"** section
4. Check **"Recent Alerts"** for any threshold breaches
5. **Expected**: Metrics displayed with breach status

**Verification**:
- ✅ VaR value shown with threshold comparison
- ✅ LCR % calculated correctly
- ✅ Color-coded (green=normal, red=breached)
- ✅ Alerts show metric type, value, threshold
- ✅ Progress bars show utilization

---

## 🧪 API Testing (Postman/curl)

### Test: Create FX Rate
```bash
curl -X POST http://localhost:5176/api/fxrate \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "baseCurrency": "GHS",
    "targetCurrency": "USD",
    "buyRate": 11.50,
    "sellRate": 11.60,
    "midRate": 11.55,
    "rateDate": "2025-02-25T00:00:00Z",
    "source": "Manual"
  }'
```

### Test: Create Treasury Position
```bash
curl -X POST http://localhost:5176/api/treasuryposition \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "positionDate": "2025-02-25",
    "currency": "GHS",
    "openingBalance": 1000000,
    "deposits": 500000,
    "withdrawals": 300000,
    "fxGainsLosses": 0,
    "closingBalance": 1200000
  }'
```

### Test: Create FX Trade
```bash
curl -X POST http://localhost:5176/api/fxtrading \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "tradeDate": "2025-02-25",
    "valueDate": "2025-02-27",
    "tradeType": "Spot",
    "direction": "Buy",
    "baseCurrency": "USD",
    "baseAmount": 100000,
    "counterCurrency": "GHS",
    "counterAmount": 1150000,
    "exchangeRate": 11.50
  }'
```

### Test: Create Investment
```bash
curl -X POST http://localhost:5176/api/investment \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "investmentType": "T-Bills",
    "instrument": "91-day Treasury Bill",
    "currency": "GHS",
    "principalAmount": 500000,
    "interestRate": 18.5,
    "discountRate": 17.8,
    "placementDate": "2025-02-25",
    "maturityDate": "2025-05-27",
    "tenorDays": 91,
    "status": "Pending"
  }'
```

### Test: Calculate Risk Metrics
```bash
curl -X POST http://localhost:5176/api/riskanalytics/var \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "metricDate": "2025-02-25",
    "currency": "GHS",
    "confidenceLevel": 95,
    "timeHorizonDays": 1
  }'
```

---

## 📋 Checklist for Complete Testing

### Backend Services
- [ ] FxRateService: Create, update, list, convert, sync (sample data)
- [ ] TreasuryPositionService: Create, update, reconcile, summary, close
- [ ] FxTradingService: Create, approve, settle, cancel, get stats
- [ ] InvestmentService: Create, approve, rollover, liquidate, mature, accrual
- [ ] RiskAnalyticsService: Calculate VaR, LCR, exposure, daily batch

### Frontend Components
- [ ] FxRateManagement: Rate entry, conversion, rate display
- [ ] TreasuryPositionMonitor: Currency selection, position view, history
- [ ] FxTradingDesk: Trade entry, approval queue, history with P&L
- [ ] InvestmentPortfolio: Summary cards, type breakdown, maturity calendar
- [ ] RiskDashboard: VaR/LCR display, currency exposure, alerts

### Integration
- [ ] Login → Treasury navigation → Component load
- [ ] Token persistence (verify 2000-char column)
- [ ] All endpoints return proper JWT authorization
- [ ] Component data refresh on tab change
- [ ] Error handling for API failures

---

## 🔍 Common Issues & Solutions

### Issue: API returns 401 Unauthorized
**Solution**: 
1. Ensure token is being sent in Authorization header
2. Login again to get fresh token: `admin@bankinsight.local` / `password123`
3. Check token format: `Bearer {token_value}`

### Issue: Token storage error (old issue, now fixed)
**Solution**: Already resolved by migration `20260225164058_IncreaseTokenLength`
- User_sessions.token column now VARCHAR(2000)

### Issue: Treasury tab not showing in sidebar
**Solution**:
1. Check App.tsx has DollarSign import
2. Verify TreasuryManagementHub is imported
3. Verify 'treasury' in activeTab type union
4. Clear browser cache and reload

### Issue: Components not loading data
**Solution**:
1. Verify API is running: `http://localhost:5176/swagger`
2. Check browser console for CORS errors
3. Verify token in localStorage (Dev Tools → Application → Storage)
4. Check API response in Network tab

---

## 📈 Performance Notes

### Optimization Tips
- FX rates: Auto-refresh every 30 seconds (configurable)
- Treasury position: Auto-refresh every 30 seconds
- FX trades: Auto-refresh every 30 seconds
- Risk dashboard: Auto-refresh every 60 seconds
- Investment: No auto-refresh (manual)

### Database Query Performance
- All trading tables use composite indexes on date + status
- FX_rates indexed by (base_currency, target_currency, rate_date)
- Treasury_positions indexed by (position_date, currency)
- Investments indexed by (maturity_date) for maturity calendar
- Risk_metrics indexed by (metric_date, metric_type)

---

## 🔐 Security Testing

### Test Authorization
1. Create test user with limited permissions
2. Try accessing treasury endpoints
3. **Expected**: Should get 403 Forbidden if missing ACCOUNT_READ permission

### Test Token Expiration
1. Login and get token
2. Wait for token to expire (or manually expire in DB)
3. Try API call with expired token
4. **Expected**: Should get 401 Unauthorized

---

## 📞 Support Resources

**API Swagger UI**: http://localhost:5176/swagger

**Database**: PostgreSQL bankinsight-postgres
```bash
# Connect to database
docker exec -it bankinsight-postgres psql -U postgres -d bankinsight

# Check treasury tables
\dt fx_rates treasury_positions fx_trades investments risk_metrics

# Query latest FX rate
SELECT * FROM fx_rates WHERE is_active = true ORDER BY rate_date DESC LIMIT 10;
```

**Logs**: Check API console output for errors

---

## ✨ What's Next?

After Phase 2 testing is complete:

1. **Phase 3: Advanced Reporting**
   - Generate regulatory returns for Bank of Ghana
   - Create financial statements (Balance Sheet, P&L)
   - Build analytics dashboards (CLV, churn, segmentation)

2. **Production Hardening**
   - Configure actual Bank of Ghana API integration
   - Set up batch job scheduling (Hangfire)
   - Implement email notifications for alerts
   - Add audit logging for all treasury operations

3. **Advanced Features**
   - Portfolio rebalancing recommendations
   - Stress testing scenarios
   - Compliance reporting automation
   - Real-time position monitoring alerts

---

*Phase 2 Treasury Management - Ready for Testing*  
*Last Updated: 2025-02-25*
