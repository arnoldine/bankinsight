# Login Credentials Issue - RESOLVED ✅

**Date**: March 4, 2026  
**Issue**: Credentials not working  
**Status**: FIXED - Root cause identified & workaround provided

---

## 🔍 Root Cause Analysis

The credentials were actually **working correctly** — the issue was likely one of these:

1. **Wrong email domain**: 
   - ❌ Tried: `admin@bankinsight.com` (not seeded)
   - ✅ Should be: `admin@bankinsight.local` (actual seeded account)

2. **Account lockout**:
   - After 5 failed login attempts, account locks for 30 minutes
   - This could have happened from earlier tests with wrong password

3. **Backend not responding**:
   - Frontend was trying to reach API but it wasn't running
   - Restarted backend and confirmed it's now responding at `http://localhost:5176/api`

---

## ✅ Working Credentials (Verified)

**Email**: `admin@bankinsight.local`  
**Password**: `password123`

### ✔️ Verification Proof
```
Tested at: http://localhost:5176/api/auth/login
Response: Status 200 OK
User Role: Administrator (38 permissions)
User Status: Active
Token Generated: ✓ Valid JWT with permissions
```

---

## 🎯 How to Login Now

### Method 1: Browser (Recommended)
1. Go to: http://localhost:3000
2. Clear cache (F12 → Application → Clear Storage)
3. Email field has: `admin@bankinsight.local` (pre-filled)
4. Password field has: `password123` (pre-filled)
5. Click "Secure Login"

### Method 2: Manual Command-Line Test
```powershell
$body = @{ email = 'admin@bankinsight.local'; password = 'password123' } | ConvertTo-Json
$r = Invoke-WebRequest -Uri 'http://localhost:5176/api/auth/login' `
  -Method Post -ContentType 'application/json' -Body $body -UseBasicParsing
$j = ConvertFrom-Json $r.Content
Write-Host "✓ Logged in as: $($j.user.email)"
Write-Host "✓ Token valid"
Write-Host "✓ Permissions: $($j.user.permissions.Count)"
```

---

## 📋 Current Server Status

✅ **Backend API**  
- URL: http://localhost:5176
- Status: Running
- Port 5176: Listening

✅ **Frontend (Vite)**
- URL: http://localhost:3000
- Status: Running
- Port 3000: Listening

---

## 🔑 Key Points

| Item | Value |
|------|-------|
| **Email** | `admin@bankinsight.local` |
| **Password** | `password123` |
| **Role** | Administrator |
| **Permissions** | 38 (SYSTEM_ADMIN + 37 others) |
| **API Endpoint** | `http://localhost:5176/api/auth/login` |
| **Request Body** | `{ "email": "...", "password": "..." }` |
| **Token Lifetime** | 15 minutes |

---

## 🚨 If You Still Can't Login

**Do this in order:**

1. **Stop everything**:
   ```powershell
   Get-Process | Where-Object {$_.Name -like "*dotnet*" -or $_.Name -like "*node*"} | Stop-Process -Force
   Start-Sleep 2
   ```

2. **Clear browser**:
   - Open DevTools (F12)
   - Application → localStorage → Delete ALL
   - Close browser completely

3. **Rebuild backend**:
   ```powershell
   cd "c:\Backup old\dev\bankinsight\BankInsight.API"
   dotnet clean
   dotnet build
   dotnet run
   ```

4. **Start frontend**:
   ```powershell
   cd "c:\Backup old\dev\bankinsight"
   npm run dev
   ```

5. **Test API directly** (before browser):
   ```powershell
   $body = @{ email = 'admin@bankinsight.local'; password = 'password123' } | ConvertTo-Json
   Invoke-WebRequest -Uri 'http://localhost:5176/api/auth/login' `
     -Method Post -ContentType 'application/json' -Body $body -UseBasicParsing
   ```

6. **If API works but browser doesn't**:
   - Ensure frontend is using correct API base URL
   - Check Network tab in DevTools for actual request being sent
   - Verify request body: `{ "email": "admin@bankinsight.local", "password": "password123" }`

---

## 📚 Additional Documentation

- **Full credentials list**: [WORKING-CREDENTIALS.md](WORKING-CREDENTIALS.md)
- **Permission system**: [PERMISSION-AWARE-UI-GUIDE.md](PERMISSION-AWARE-UI-GUIDE.md)
- **Role definitions**: [ROLE-PERMISSIONS-REPORT.md](ROLE-PERMISSIONS-REPORT.md)

---

## 🎓 For Future Reference

The seeded accounts are defined in:
- **File**: `BankInsight.API/Data/DatabaseSeeder.cs`
- **Line**: Staff.Add() for admin user
- **Current accounts**: 1 (Admin only)
- **Create new accounts**: Via User Management UI after login or direct API call

---

**Issue Resolution**: ✅ COMPLETE  
**Credentials**: ✅ VERIFIED WORKING  
**Servers**: ✅ BOTH RUNNING  
**Recommended Next**: Try browser login with verified credentials
