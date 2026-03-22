# Permission-Aware UI Integration Guide

## Overview

The permission-aware UI system enables the frontend to dynamically render components and features based on the user's JWT permissions. This creates a seamless experience where the UI adapts to each user's role and permissions.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│  JWT Token (from Backend at Login)                  │
│  - Includes "permissions" claim (array of strings) │
│  - Stored in localStorage: "bankinsight_token"     │
└────────────────┬────────────────────────────────────┘
                 │
     ┌───────────▼─────────────┐
     │  jwtUtils.ts (Utilities)│
     │  - decodeJWT()          │
     │  - hasPermission()      │
     │  - getRoleFromToken()   │
     └───┬───────────────────┬─┘
         │                   │
    ┌────▼─────┐      ┌─────▼──────┐
    │Permission│      │PermissionGuard
    │Guard     │      │ Component
    │Component │      └────────────┘
    └────┬─────┘
         │
    ┌────▼──────────────────────────┐
    │  Role-Based Dashboard Comps   │
    │  - AdminDashboard             │
    │  - TellerDashboard            │
    │  - BranchManagerDashboard     │
    │  - RoleBasedDashboard (Header)│
    └───────────────────────────────┘
```

## Components

### 1. jwtUtils.ts - JWT Parsing Utilities

**Location**: `lib/jwtUtils.ts`

**Key Functions**:
```typescript
// Decode JWT without signature verification (client-side only)
decodeJWT(token: string): JWTPayload | null

// Check single permission
hasPermission(token: string, permission: string): boolean

// Check multiple permissions (OR logic)
hasAnyPermission(token: string, permissions: string[]): boolean

// Check multiple permissions (AND logic)
hasAllPermissions(token: string, permissions: string[]): boolean

// Extract permission array
getPermissionsFromToken(token: string): string[]

// Extract user role
getRoleFromToken(token: string): string | null

// Check if token expired
isTokenExpired(token: string): boolean
```

**Usage**:
```typescript
import { hasPermission, getPermissionsFromToken } from '../lib/jwtUtils';

const token = localStorage.getItem('bankinsight_token');
const permissions = getPermissionsFromToken(token);
const isAdmin = hasPermission(token, 'SYSTEM_ADMIN');
```

### 2. PermissionGuard.tsx - Conditional Rendering Component

**Location**: `components/PermissionGuard.tsx`

**Props**:
```typescript
interface PermissionGuardProps {
  permission: string | string[];      // Single permission or array
  requireAll?: boolean;                // AND logic (true) vs OR logic (false)
  fallback?: React.ReactNode;          // UI to show if denied
  token?: string;                      // Optional token (uses localStorage if not provided)
  children: React.ReactNode;           // Content to show if allowed
}
```

**Usage - Single Permission**:
```typescript
<PermissionGuard permission="MANAGE_USERS">
  <button>Manage Users</button>
</PermissionGuard>
```

**Usage - Multiple Permissions (OR Logic)**:
```typescript
<PermissionGuard permission={['LOAN_APPROVE', 'SYSTEM_ADMIN']}>
  <ApprovalPanel />
</PermissionGuard>
```

**Usage - Multiple Permissions (AND Logic)**:
```typescript
<PermissionGuard 
  permission={['SYSTEM_CONFIG', 'REPORT_VIEW']} 
  requireAll={true}
>
  <AdvancedReporting />
</PermissionGuard>
```

**Usage - With Fallback UI**:
```typescript
<PermissionGuard 
  permission="SYSTEM_ADMIN"
  fallback={<p>You don't have permission to access this feature</p>}
>
  <AdminPanel />
</PermissionGuard>
```

### 3. Role-Based Dashboard Components

#### RoleBasedDashboard.tsx
**Location**: `components/RoleBasedDashboard.tsx`

Displays role information, permission count, and role features at the top of the dashboard.

**Features**:
- Shows current role (Admin, Teller, Branch Manager, etc.)
- Displays permission count
- Lists role-specific features
- Shows first 12 permissions (with overflow indicator)

**Usage**:
```typescript
<RoleBasedDashboard />  // Uses token from localStorage
```

#### AdminDashboard.tsx
**Location**: `components/AdminDashboard.tsx`

Specialized view for SYSTEM_ADMIN role showing:
- User & role management
- System configuration
- Security settings
- Backup & recovery
- Recent admin activity

**Usage**:
```typescript
<PermissionGuard permission="SYSTEM_ADMIN">
  <AdminDashboard />
</PermissionGuard>
```

#### TellerDashboard.tsx
**Location**: `components/TellerDashboard.tsx`

Specialized view for TELLER_POST permission showing:
- Quick transaction operations
- Daily reconciliation checklist
- Account summary cards
- Limited feature set (transactions & GL only)

**Usage**:
```typescript
<PermissionGuard permission="TELLER_POST">
  <TellerDashboard />
</PermissionGuard>
```

#### BranchManagerDashboard.tsx
**Location**: `components/BranchManagerDashboard.tsx`

Specialized view for LOAN_APPROVE permission showing:
- Pending loan approvals
- Branch performance metrics
- Staff overview
- Approval workflows

**Usage**:
```typescript
<PermissionGuard permission="LOAN_APPROVE">
  <BranchManagerDashboard />
</PermissionGuard>
```

## Integration Points

### 1. Update useBankingSystem Hook

**File**: `hooks/useBankingSystem.ts`

The hook's `hasPermission()` function has been updated to use jwtUtils:

```typescript
import { hasPermission as checkPermission } from '../lib/jwtUtils';

const hasPermission = (permission: Permission): boolean => {
    if (!authToken) return false;
    return checkPermission(authToken, permission);
};
```

This enables permission checking throughout the app via the hook.

### 2. Sidebar Permission Filtering

**File**: `App.tsx` (lines ~260-280)

The SidebarItem component already has permission support:

```typescript
const SidebarItem = ({ id, icon: Icon, label, permission }) => {
    if (permission && !hasPermission(permission as any)) return null;
    return (
        <button onClick={() => setActiveTab(id)} ...>
            {/* Item content */}
        </button>
    );
};
```

This now correctly hides sidebar items based on user permissions.

### 3. Dashboard Integration

**File**: `App.tsx` (line ~377)

The RoleBasedDashboard component has been added to the dashboard view:

```typescript
{activeTab === 'dashboard' && (
    <div>
        <RoleBasedDashboard />  {/* Role info header */}
        {/* ... rest of dashboard */}
    </div>
)}
```

## Permission System Architecture

### JWT Token Structure
```json
{
  "sub": "user-id",
  "email": "admin@bankinsight.com",
  "role_id": "ROL0001",
  "role_name": "Administrator",
  "permissions": [
    "SYSTEM_ADMIN",
    "ACCOUNT_READ",
    "LOAN_READ",
    // ... 35 more permissions
  ],
  "exp": 1234567890,
  "iat": 1234567890
}
```

### Available Permissions by Role

**Admin (SYSTEM_ADMIN)**
- 38 permissions including SYSTEM_ADMIN
- Full system access

**Teller (TELLER_POST)**
- `TELLER_POST` - Post deposits/withdrawals
- `POST_TRANSACTION` - General transaction posting
- Limited to transaction operations

**Branch Manager (LOAN_APPROVE)**
- `LOAN_APPROVE` - Approve loans
- `ACCOUNT_READ` - View accounts
- `POST_TRANSACTION` - Post transactions
- `REPORT_VIEW` - View reports

**Manager**
- 6 permissions
- Intermediate access level

**Analyst (REPORT_VIEW)**
- `REPORT_VIEW` - View reports
- Analytics read-only access

## Data Flow

### 1. User Login
- User enters email/password in LoginScreen
- Backend validates and returns JWT token with permissions
- Frontend stores token in localStorage

### 2. Sidebar Rendering
- App.tsx queries hasPermission() for each sidebar item
- useBankingSystem.js calls jwtUtils.hasPermission()
- jwtUtils decodes JWT and checks against permission array
- SidebarItem returns null if permission denied
- User sees filtered sidebar menu

### 3. Feature Access
- Whenever showing a restricted feature, wrap with `<PermissionGuard>`
- Component checks token permissions
- Shows feature or fallback UI

### 4. Role-Specific Dashboards
- RoleBasedDashboard determines user's role from token
- Displays role-specific welcome card
- Routes to appropriate dashboard (Admin/Teller/Branch Manager)
- Each dashboard shows role-appropriate features

## Testing the System

### Run the Permission-Aware UI Tests
```powershell
.\permission-aware-ui-test.ps1
```

This tests:
- JWT parsing (decodeJWT)
- Permission checking (hasPermission)
- Multi-permission logic (AND/OR)
- Dashboard access guards
- Token expiration

### Manual Testing

1. **Clear browser storage**:
   - Open DevTools > Application > localStorage
   - Delete "bankinsight_token" key

2. **Login as Admin**:
   - Email: admin@bankinsight.com
   - Password: Admin@12345
   - Verify: All sidebar items visible, AdminDashboard shown

3. **Login as Teller**:
   - Email: teller@bankinsight.com
   - Password: Teller@12345
   - Verify: Limited sidebar (Teller, Transactions only), TellerDashboard shown

4. **Login as Branch Manager**:
   - Email: branch_mgr@bankinsight.com
   - Password: BranchMgr@12345
   - Verify: Approvals visible, BranchManagerDashboard shown

5. **Check Console**:
   - Open DevTools > Console
   - Look for permission check logs
   - No errors when decoding JWT

## Common Integration Patterns

### Pattern 1: Conditional Button Visibility
```typescript
<PermissionGuard permission="SYSTEM_CONFIG">
  <button>System Settings</button>
</PermissionGuard>
```

### Pattern 2: Feature Sections
```typescript
<PermissionGuard permission="REPORT_VIEW">
  <ReportSection />
</PermissionGuard>
```

### Pattern 3: Multi-Permission Features
```typescript
<PermissionGuard permission={['SYSTEM_ADMIN', 'LOAN_APPROVE']} requireAll={false}>
  <ApprovalSystem />
</PermissionGuard>
```

### Pattern 4: Admin-Only UI
```typescript
<PermissionGuard permission="SYSTEM_ADMIN" fallback={<p>Admin only</p>}>
  <AdminControls />
</PermissionGuard>
```

### Pattern 5: Using Hook
```typescript
const { hasPermission } = useBankingSystem();

if (hasPermission('SYSTEM_CONFIG')) {
    return <SettingsPanel />;
}
```

## Security Notes

⚠️ **Important Security Considerations**:

1. **Client-side checking is NOT a security boundary**
   - JWT parsing on client-side is for UX only
   - Backend MUST verify permissions for all API calls
   - Backend returns HTTP 403 for unauthorized access

2. **Tokens are stored in localStorage**
   - Accessible to JavaScript
   - Vulnerable to XSS attacks
   - Consider using HttpOnly cookies in production

3. **Permissions are embedded in JWT**
   - No server roundtrip for permission checks
   - Reduces latency but requires token refresh for permission changes
   - Token lifetime is 15 minutes (auto-refresh on re-login)

4. **SYSTEM_ADMIN is a bypass permission**
   - Admins can access any feature
   - Check backend enforcement, not just client-side

## Troubleshooting

### Tokens Not Decoding
```javascript
// In browser console:
const token = localStorage.getItem('bankinsight_token');
console.log('Token:', token);
console.log('Decoded:', jwt_decode(token)); // If using jwt-decode library
```

### Sidebar Items Still Visible After Permission Denial
- Ensure SidebarItem checks `hasPermission()` correctly
- Verify hasPermission hook is connected to jwtUtils
- Check localStorage has valid token

### Role Dashboard Not Showing
- Verify RoleBasedDashboard is imported in App.tsx
- Check that token has role_id in payload
- Ensure role-to-permission mapping is correct

### Permission Guard Not Working
- Verify token in localStorage (check DevTools)
- Confirm permission name matches backend exactly
- Check for case sensitivity (permissions are case-sensitive)

## Performance Optimization

- JWT decoding happens once per component mount
- Permission checks are O(n) array lookups
- Memoize permission checks if used in loops:

```typescript
const isBranchManager = useMemo(
    () => hasPermission(token, 'LOAN_APPROVE'),
    [token]
);
```

## Next Steps

1. ✅ JWT utilities created (jwtUtils.ts)
2. ✅ PermissionGuard component created
3. ✅ useBankingSystem hook updated
4. ✅ Role-based dashboards created
5. 🔄 **Integration with Settings and Admin sections**
6. 🔄 **Wrap all restricted components with PermissionGuard**
7. 🔄 **Test complete user journeys for each role**
8. 🔄 **User acceptance testing with actual roles**

## References

- [JWT Structure](../types.ts) - JWTPayload interface
- [Role Definitions](../ROLE-PERMISSIONS-REPORT.md) - Complete role reference
- [Backend Permission Enforcement](../BankInsight.API/Controllers/) - API-level checks
- [RBAC Tests](../permission-enforcement-test.ps1) - Backend validation tests

---

**Last Updated**: March 3, 2026  
**Status**: Permission-Aware UI System Integrated ✓
