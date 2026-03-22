# BankInsight Integration Verification Checklist

## Pre-Launch Verification

### ✅ Environment Setup

- [ ] Node.js 18+ installed: `node --version`
- [ ] npm installed: `npm --version`
- [ ] .NET 8.0 SDK installed: `dotnet --version`
- [ ] PostgreSQL running on default port
- [ ] Git configured: `git config --global user.name`

### ✅ Backend Verification

#### Dependencies
- [ ] Run: `cd BankInsight.API && dotnet restore`
- [ ] Verify no restore errors
- [ ] Check bin/ directory populated

#### Database
- [ ] PostgreSQL connection configured in appsettings.json
- [ ] Run: `dotnet ef database update`
- [ ] Verify all migrations applied
- [ ] Check database tables created

#### Server
- [ ] Run: `dotnet run`
- [ ] Verify port 5176 accessible
- [ ] Check console for "Application started"
- [ ] No compilation errors

#### API Testing
- [ ] Open browser to: http://localhost:5176/api/auth/validate
- [ ] API responds (may get 401 - that's OK)
- [ ] Response headers include performance header

### ✅ Frontend Verification

#### Dependencies
- [ ] Run: `npm install`
- [ ] Verify no install errors
- [ ] Package-lock.json updated

#### Configuration
- [ ] .env.local exists with VITE_API_URL
- [ ] Check: `cat .env.local | grep VITE_API_URL`
- [ ] Shows: `VITE_API_URL=http://localhost:5176/api`

#### Files
- [ ] Verify service files exist:
  - [ ] src/services/apiConfig.ts
  - [ ] src/services/httpClient.ts
  - [ ] src/services/authService.ts
  - [ ] src/services/reportService.ts
  - [ ] src/services/treasuryService.ts

- [ ] Verify hook files exist:
  - [ ] src/hooks/useApi.ts

- [ ] Verify component files exist:
  - [ ] src/components/DashboardLayout.tsx
  - [ ] src/components/LoginScreen.tsx
  - [ ] src/components/ReportingHub.tsx
  - [ ] src/components/TreasuryManagementHub.tsx
  - [ ] src/components/ErrorBoundary.tsx

#### Build
- [ ] Run: `npm run build`
- [ ] Verify no build errors
- [ ] Check dist/ directory created
- [ ] dist/ contains index.html and js files

#### Development Server
- [ ] Run: `npm run dev`
- [ ] Wait for "Local: http://localhost:3000"
- [ ] Browser auto-opens (or manually open)
- [ ] Page loads without console errors

### ✅ Integration Testing

#### Login Screen Display
- [ ] Login page appears
- [ ] Email field visible
- [ ] Password field visible
- [ ] Sign In button visible
- [ ] Demo credentials shown

#### Login Flow
- [ ] Enter: admin@bankinsight.com
- [ ] Enter: Admin@123
- [ ] Click Sign In
- [ ] Loading spinner appears
- [ ] Dashboard loads after ~2 seconds
- [ ] No console errors

#### Dashboard Display
- [ ] Sidebar visible on left
- [ ] Navigation items visible (Dashboard, Treasury, Reports, etc.)
- [ ] Header shows current tab
- [ ] User info displays in sidebar
- [ ] Logout button visible

#### Dashboard Content
- [ ] Welcome message visible
- [ ] Status cards display (System Status, API Status, etc.)
- [ ] Feature list visible
- [ ] System information section visible

#### Navigation
- [ ] Click "Treasury" tab
- [ ] Treasury page loads
- [ ] Positions, FX Trades tabs visible
- [ ] Back to Dashboard tab works
- [ ] Component switches without errors

#### Reports Tab
- [ ] Click "Reports" tab
- [ ] Report list displays
- [ ] Category filters visible
- [ ] Reports load without errors

#### Error Handling
- [ ] Open browser DevTools (F12)
- [ ] Check Console tab
- [ ] No red error messages
- [ ] Network tab shows API calls
- [ ] All API responses have 2xx or 401 status

#### LocalStorage
- [ ] Open DevTools → Application tab
- [ ] Expand LocalStorage
- [ ] Check auth_token exists
- [ ] Check auth_user exists with user data

#### Logout
- [ ] Click Logout button
- [ ] Loading spinner appears
- [ ] Redirects to Login screen
- [ ] LocalStorage cleared (check DevTools)

### ✅ API Integration Testing

#### Open DevTools → Network Tab
- [ ] Performs login call: POST /api/auth/login
- [ ] Response includes token in body
- [ ] Response includes user object

#### Check Headers
- [ ] Click any API call in Network tab
- [ ] Response headers include:
  - [ ] X-Response-Time-Ms (performance header)
  - [ ] Content-Type: application/json
  - [ ] Other standard headers

#### Test Rate Limiting (Optional)
- [ ] Console: 
  ```javascript
  for(let i=0; i<70; i++) {
    fetch('http://localhost:5176/api/auth/validate',
      {headers: {'Authorization': 'Bearer invalid'}}).catch(e=>0);
  }
  ```
- [ ] After 60 requests, should see 429 responses

#### Test Error Handling
- [ ] Modify .env.local: `VITE_API_URL=http://invalid:999/api`
- [ ] Reload page
- [ ] Should show error gracefully
- [ ] Try Again button functional (if implemented)
- [ ] Restore .env.local when done

### ✅ Performance Verification

#### Response Times
- [ ] Check Network tab for API calls
- [ ] Most requests < 500ms
- [ ] Long requests logged with X-Response-Time-Ms header

#### Network Performance
- [ ] Load Dashboard page
- [ ] Measure total load time
- [ ] Should be < 5 seconds
- [ ] All assets should load

#### Memory Usage
- [ ] Open DevTools → Memory tab
- [ ] Take heap snapshot
- [ ] Page should use < 100MB RAM
- [ ] No obvious memory leaks

### ✅ Security Verification

#### Token Management
- [ ] Token stored in localStorage (not cookies)
- [ ] Token in Authorization header for all requests
- [ ] Token format: Bearer eyJhbGci...

#### CORS
- [ ] Requests from http://localhost:3000 succeed
- [ ] Would fail from other origins (check appsettings.json)

#### Authentication
- [ ] Invalid credentials rejection works
- [ ] 401 responses trigger logout
- [ ] Token expiry handling implemented

### ✅ Browser Compatibility

Test in multiple browsers:
- [ ] Chrome/Edge (latest)
- [ ] Firefox (latest)
- [ ] Safari (if on Mac)

All should display properly without errors.

### ✅ TypeScript Compilation

```bash
# In src/ directory
npx tsc --noEmit
```
- [ ] Zero TypeScript errors
- [ ] No strict mode violations

### ✅ Code Quality

```bash
# Check for obvious issues
grep -r "console.error" src/ --exclude-dir=node_modules
grep -r "any\>" src/services/ --exclude-dir=node_modules
```
- [ ] Minimal console errors
- [ ] Type safety maintained

## Troubleshooting Guide

### "Cannot GET /api/..."
- **Problem**: Backend not running
- **Solution**: 
  ```bash
  cd BankInsight.API
  dotnet run
  ```

### "Login fails with CORS error"
- **Problem**: Backend CORS not configured
- **Solution**: Check appsettings.json has localhost:3000 in CORS origins

### "NetworkError when fetching resource"
- **Problem**: Backend URL wrong in .env
- **Solution**: Verify VITE_API_URL=http://localhost:5176/api

### "Blank page after login"
- **Problem**: DashboardLayout or dependencies missing
- **Solution**: Verify all component files exist and imports correct

### "Module not found errors"
- **Problem**: Dependencies not installed
- **Solution**: 
  ```bash
  npm install
  npm run dev
  ```

### "Port 3000 already in use"
- **Problem**: Another app using port
- **Solution**:
  ```bash
  # Kill process on port 3000
  # Windows: netstat -ano | findstr :3000
  # Linux/Mac: lsof -i :3000
  ```

### "Port 5176 already in use"
- **Problem**: Another API instance running
- **Solution**: Kill existing process or use `dotnet run --urls=http://localhost:5177`

## Final Sign-Off

When all checks pass:

```
✅ Backend API running and responding
✅ Frontend serving on localhost:3000
✅ Login authentication working
✅ Dashboard displaying correctly
✅ Navigation functioning
✅ API calls successful
✅ Error handling working
✅ No console errors
✅ Performance acceptable
✅ Security checks passed
```

**System is READY FOR PRODUCTION DEPLOYMENT**

---

## Quick Restart Script

Create `start-bankinsight.sh` (Linux/Mac) or `start-bankinsight.bat` (Windows):

### Linux/Mac:
```bash
#!/bin/bash
# Terminal 1
cd BankInsight.API
dotnet run &

# Terminal 2
npm run dev
```

### Windows PowerShell:
```powershell
# Run both in separate terminals
# Terminal 1:
cd BankInsight.API; dotnet run

# Terminal 2:
npm run dev
```

---

**Integration Verification Complete!**
For issues, check the error messages and refer to PHASE-4-FRONTEND-BACKEND-INTEGRATION.md
