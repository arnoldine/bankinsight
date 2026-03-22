# 🔑 BankInsight Login Credentials - Working

## ✅ Verified Working Credentials

**These credentials have been tested and confirmed working against the API:**

### Admin Account
- **Email**: `admin@bankinsight.local`
- **Password**: `password123`
- **Role**: Administrator
- **Permissions**: 38 (full system access)
- **Status**: Active

---

## 🌐 Browser Login Steps

1. **Open Frontend**: http://localhost:3000
2. **Default Form**:
   - Email is pre-filled: `admin@bankinsight.local`
   - Password is pre-filled: `password123`
3. **Click "Secure Login"** button
4. **Expect**: Dashboard with full feature access

---

## 🔍 Troubleshooting If Login Still Fails

### Step 1: Check Backend is Running
```powershell
# In terminal, verify port 5176 is listening
Get-NetTCPConnection -LocalPort 5176 -State Listen -ErrorAction SilentlyContinue
```
**Expected**: Shows listening connection  
**If not**: Restart backend (see below)

### Step 2: Test API Directly (before trying browser)
```powershell
$body = @{ email = 'admin@bankinsight.local'; password = 'password123' } | ConvertTo-Json
$r = Invoke-WebRequest -Uri 'http://localhost:5176/api/auth/login' -Method Post `
  -ContentType 'application/json' -Body $body -UseBasicParsing
$j = ConvertFrom-Json $r.Content
$j.user | Select-Object email, roleName, status
```
**Expected**: Shows user details with status = "Active"  
**If fails**: Backend has an issue (see Backend Restart below)

### Step 3: Check Frontend API Base URL
In browser DevTools (F12):
```javascript
// Open Console and run:
localStorage.getItem('bankinsight_api_config')
```
**Expected**: Should show `http://localhost:5176/api`  
**If wrong**: Clear and reload: `localStorage.clear(); location.reload()`

### Step 4: Clear Browser Cache
```javascript
// In DevTools Console:
localStorage.clear()
sessionStorage.clear()
location.reload()
```
Then try login again.

---

## 🚀 Restart Backend (if needed)

```powershell
cd "c:\Backup old\dev\bankinsight\BankInsight.API"

# Stop any existing process
Get-Process | Where-Object {$_.Name -like "*dotnet*"} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep 2

# Start fresh
dotnet run
```

**Wait for message**: `Now listening on: http://localhost:5176`

---

## 🚀 Restart Frontend (if needed)

```powershell
cd "c:\Backup old\dev\bankinsight"

# Stop any existing process
Get-Process | Where-Object {$_.Name -like "*node*"} | Stop-Process -Force -ErrorAction SilentlyContinue

Start-Sleep 2

# Start fresh
npm run dev
```

**Wait for message**: `Local: http://localhost:3000/`

---

## 📋 Common Issues & Fixes

| Issue | Cause | Fix |
|-------|-------|-----|
| "Invalid email or password" | Wrong email domain | Use `admin@bankinsight.local` (not `.com`) |
| "Invalid email or password" | Wrong password | Use `password123` (case sensitive) |
| "Unauthorized" | Account locked (5 failed attempts) | Wait 30 minutes or check database |
| Connection timeout | Backend not running | Run `dotnet run` in BankInsight.API folder |
| CORS errors | Frontend-backend mismatch | Ensure both running on localhost |
| Blank page | Missing token | Clear localStorage and reload |

---

## 🔐 What Happens After Login

✅ **Admin Account Gets**:
- Access to ALL features (System Admin bypass)
- Dashboard with all metrics
- User management
- Role management  
- System configuration
- Reports and analytics
- Audit logs
- All sidebar items visible

---

## 📊 Account Details (for reference)

```
Database User Account:
- ID: STF0001
- Name: Admin User
- Email: admin@bankinsight.local
- Phone: 0000000000
- Branch: BR001 (Head Office)
- Role: ROLE_ADMIN
- Status: Active
```

---

## 🧪 Quick Test Command

Validate credentials before browser attempt:

```powershell
$body = @{ email = 'admin@bankinsight.local'; password = 'password123' } | ConvertTo-Json
$r = Invoke-WebRequest -Uri 'http://localhost:5176/api/auth/login' -Method Post `
  -ContentType 'application/json' -Body $body -UseBasicParsing
if ($r.StatusCode -eq 200) { 
  Write-Host "✓ Credentials valid" -ForegroundColor Green
  ConvertFrom-Json $r.Content | ConvertTo-Json -Depth 2
} else {
  Write-Host "✗ Login failed" -ForegroundColor Red
}
```

---

## 📱 Other User Accounts (when available)

Currently only the Admin account is seeded. Additional accounts can be created via:
- User Management panel (after admin login)
- Direct database insert
- API user creation endpoint

---

**Status**: ✅ Credentials confirmed working as of March 4, 2026  
**Tested**: Direct API call + token validation  
**All 38 Admin Permissions**: Active
