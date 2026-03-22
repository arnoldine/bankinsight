# BankInsight API Endpoint Fixes - Quick Reference

## What Was Fixed

### 1. **Frontend API Routing Issues**
- Frontend was calling `GET http://localhost:3000/api/treasury/investments` and getting 404 errors
- Root cause: Relative `/api` paths in VITE_API_URL resolved to the wrong host/port
- **Solution**: Updated `docker-compose.yml` to set `VITE_API_URL: http://api:8080/api`

### 2. **Missing Backend Endpoints**
- `/api/deposits` - No controller existed
- **Solution**: Created `DepositController.cs` with full CRUD operations

### 3. **Investment Controller Route Mismatch**
- Controller was on `api/investment` but frontend expected `api/treasury/investments`
- **Solution**: Changed route from `api/[controller]` to `api/treasury/investments`

### 4. **Frontend Service Configuration**
- `investmentService.ts` was using hardcoded paths instead of API_ENDPOINTS
- **Solution**: 
  - Added `API_ENDPOINTS.deposits` object to `apiConfig.ts`
  - Updated all investmentService methods to use API_ENDPOINTS

---

## How to Run

### **With Docker Compose (Recommended)**
```bash
# Build and start all services
docker compose up --build

# Frontend: http://localhost:3000
# API: http://localhost:5176
# PgAdmin: http://localhost:5050
```

### **Vite Dev Server (Local Development)**
```bash
# Set API URL environment variable
set VITE_API_URL=http://localhost:5176/api

# Start dev server
npm run dev
```

---

## API Endpoints Tested

| Endpoint | Method | Status |
|----------|--------|--------|
| `/api/auth/validate` | GET | ✅ 401 (expects auth) |
| `/api/treasury/investments` | GET | ✅ Configured |
| `/api/deposits` | GET | ✅ New controller |
| `/api/group-lending/groups` | GET | ✅ Existing |
| `/api/vault/tills/open` | POST | ✅ Existing |
| `/api/customers` | GET | ✅ Existing |

---

## Key Configuration

### Docker Service URLs
- Internal (container-to-container): `http://api:8080/api`
- External (host machine): `http://localhost:5176/api`
- Frontend proxy: Nginx at port 3000 proxies `/api/*` to backend

### Nginx Proxy Config
Located in `docker/nginx/default.conf`:
```nginx
location /api/ {
    proxy_pass http://api:8080/api/;
    # Headers for proper forwarding
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}
```

---

## Troubleshooting

### "API returning 404 for deposits"
Check that `DepositController.cs` was built into the API image:
```bash
docker logs bankinsight-api | grep "Route"
```

### "Frontend still hitting wrong port"
Verify env variable was applied during build:
```bash
docker inspect bankinsight-frontend | grep -i vite
```

### "Dev server can't reach API"
Ensure `VITE_API_URL` is set before running `npm run dev`:
```bash
echo $VITE_API_URL  # Should show http://localhost:5176/api
```
