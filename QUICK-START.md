# BankInsight Quick Start Guide

## ⚡ 5-Minute Setup

### Prerequisites Check (1 minute)
```bash
# Check all required tools
node --version          # Should be 18+
npm --version           # Should be 9+
dotnet --version        # Should be 8.0
```

### Backend Setup (2 minutes)

**Terminal 1:**
```bash
cd BankInsight.API
dotnet restore      # Download NuGet packages
dotnet run         # Start API server
```

Expected output:
```
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
Now listening on: http://localhost:5176
```

### Frontend Setup (2 minutes)

**Terminal 2:**
```bash
npm install        # Install npm packages (1 time only)
npm run dev       # Start development server
```

Expected output:
```
  Local:   http://localhost:3000/
  press h + enter to show help
```

### Launch (< 1 minute)

1. Browser opens at http://localhost:3000
2. Login screen appears
3. Enter credentials:
   - Email: `admin@bankinsight.com`
   - Password: `Admin@123`
4. Click "Sign In"
5. Dashboard loads!

---

## 🎯 What You Get

Once logged in, you have access to:

### Dashboard Tab
- System status overview
- API health checks
- Feature availability
- System information

### Treasury Tab
- Treasury positions
- FX rates and trades
- Investments portfolio
- Risk metrics

### Reports Tab
- Report catalog
- Financial reports
- Analytics dashboards
- Export functionality

### Users, Settings, Audit Tabs
- User management
- System settings
- Audit trail
- Configuration

---

## 🛠️ Development Workflow

### To Create a New Feature

1. **Create a React Component**
   ```tsx
   // src/components/MyFeature.tsx
   import { useReports } from '../hooks/useApi';
   
   export default function MyFeature() {
     const { getReportCatalog } = useReports();
     
     useEffect(() => {
       getReportCatalog().then(data => {
         console.log('Reports:', data);
       });
     }, []);
     
     return <div>My Feature</div>;
   }
   ```

2. **Add to Navigation**
   - Edit `src/components/DashboardLayout.tsx`
   - Add to `menuItems` array
   - Add conditional render in content area

3. **Use API Hooks**
   - Import hook: `useAuth`, `useReports`, `useTreasury`, or generic `useFetch`
   - Call methods with await
   - Handle loading/error states

4. **Test Locally**
   ```bash
   npm run dev
   # Check in browser at localhost:3000
   ```

---

## 🔍 Debugging

### View API Calls
1. Press **F12** to open DevTools
2. Go to **Network** tab
3. Perform action in app
4. Watch requests/responses
5. Check Status, Headers, Response

### Check Errors
1. Press **F12** for DevTools
2. Go to **Console** tab
3. Look for red error messages
4. Check stack traces

### Check Token
1. Press **F12** for DevTools
2. Go to **Application** tab
3. Click **LocalStorage**
4. Find `auth_token` and `auth_user`
5. Verify format and content

### Test API Separately
```javascript
// In browser console:
fetch('http://localhost:5176/api/auth/validate')
  .then(r => r.json())
  .then(d => console.log(d))
```

---

## 📊 Architecture at a Glance

```
User Interface
    ↓
React Components
    ↓
Custom Hooks (useAuth, useReports, etc.)
    ↓
TypeScript Services (authService, reportService, etc.)
    ↓
HTTP Client (with JWT injection)
    ↓
REST API (/api/...)
    ↓
C# Backend Controllers & Services
    ↓
PostgreSQL Database
```

---

## 🔐 How Authentication Works

1. **Login**
   - User enters email/password
   - LoginScreen → useAuth.login()
   - API returns JWT token
   - Token stored in localStorage

2. **Each Request**
   - httpClient reads token from localStorage
   - Adds `Authorization: Bearer <token>` header
   - API validates token
   - Request proceeds

3. **Logout**
   - Click Logout button
   - Clear localStorage
   - Redirect to login page

---

## 📈 Default Ports

| Service | URL | Notes |
|---------|-----|-------|
| Frontend | http://localhost:3000 | React app |
| Backend API | http://localhost:5176 | .NET API |
| API Endpoints | http://localhost:5176/api | RESTful API |
| PostgreSQL | localhost:5432 | Database (internal) |

---

## 🚨 Common Issues & Fixes

### "Cannot GET /api/..."
```bash
# Backend not running
# Fix: Start backend in Terminal 1
cd BankInsight.API && dotnet run
```

### "localhost:3000 refused to connect"
```bash
# Frontend not running
# Fix: Start frontend in Terminal 2
npm run dev
```

### "Port 3000 already in use"
```bash
# Another app using port
# Fix: Kill process or use different port
npm run dev -- --port 3001
```

### "Port 5176 already in use"
```bash
# Another .NET app running
# Fix: Use different port
dotnet run --urls=http://localhost:5177
# Then update .env.local to match
```

### "CORS error in console"
```
# Backend CORS not configured for frontend origin
# Check: BankInsight.API/appsettings.json
# Verify: AllowedOrigins includes http://localhost:3000
```

### "Login fails: 401"
```
# Invalid credentials or database issue
# Check console for detailed error
# Verify database is populated with users
```

### "Blank page after login"
```
# DashboardLayout component issue
# Check browser console for errors
# Verify all component files exist
```

---

## 🧪 Quick Testing

### Verify Frontend
```bash
npm run build     # Should complete with no errors
npm run test      # Should run test suite
```

### Verify Backend
```bash
cd BankInsight.API
dotnet build      # Should compile
dotnet test       # Should run tests (if xUnit configured)
```

### Test API Manually
```bash
# Terminal 3 - using curl or PowerShell
curl http://localhost:5176/api/auth/validate

# Or in PowerShell:
Invoke-WebRequest http://localhost:5176/api/auth/validate
```

---

## 📚 Key Files

| File | Purpose |
|------|---------|
| `src/AppIntegrated.tsx` | Main app entry |
| `src/components/DashboardLayout.tsx` | Main layout |
| `src/components/LoginScreen.tsx` | Login form |
| `src/services/authService.ts` | Authentication |
| `src/hooks/useApi.ts` | Custom hooks |
| `BankInsight.API/Program.cs` | Backend startup |
| `.env.local` | Frontend config |
| `appsettings.json` | Backend config |

---

## 🎓 Learning Paths

### React Developer
1. Learn `src/hooks/useApi.ts` - how hooks work
2. Explore `src/components/` - see examples
3. Try modifying `DashboardLayout.tsx`
4. Create simple component using `useReports` hook

### .NET Developer
1. Check `BankInsight.API/Controllers/`
2. Look at `Services/` business logic
3. Understand middleware in `Program.cs`
4. Try adding new endpoint

### Full-Stack Developer
1. Understand both above paths
2. See how frontend calls backend
3. Debug in DevTools Network tab
4. Try adding feature from frontend to backend

---

## 🚀 Next Steps

After confirming everything works:

1. **Build More Features**
   - Create new React components
   - Add new API endpoints
   - Extend business logic

2. **Improve Performance**
   - Add response caching
   - Optimize queries
   - Implement pagination

3. **Enhance Security**
   - Add 2FA
   - Implement rate limiting rules
   - Add audit logging

4. **Deploy to Production**
   - Build Docker images
   - Set up CI/CD
   - Deploy to cloud (AWS, Azure, etc.)

---

## 📞 Help Resources

1. **API Documentation**: See PHASE-4-FRONTEND-BACKEND-INTEGRATION.md
2. **Setup Issues**: See INTEGRATION-VERIFICATION-CHECKLIST.md
3. **Architecture**: See PHASE-4-COMPLETION-REPORT.md
4. **Code Examples**: Check src/components/*.tsx

---

## ✅ Final Checklist

Before declaring "ready":
- [ ] Backend running on localhost:5176
- [ ] Frontend running on localhost:3000
- [ ] Login works with admin credentials
- [ ] Dashboard loads after login
- [ ] Navigation tabs switch views
- [ ] API calls shown in Network tab
- [ ] No red errors in Console tab
- [ ] localStorage has auth_token

---

## 🎉 You're Ready!

The complete BankInsight banking system is now running with:
- ✅ Full authentication
- ✅ Multiple feature modules
- ✅ Professional UI
- ✅ API integration
- ✅ Error handling
- ✅ Performance monitoring

**Happy coding! 🚀**

---

For more details, see the comprehensive documentation:
- PHASE-4-FRONTEND-BACKEND-INTEGRATION.md
- PHASE-4-COMPLETION-REPORT.md
- INTEGRATION-VERIFICATION-CHECKLIST.md
