# Permission-Aware UI - Developer Quick Reference

## Quick Start (Copy-Paste Examples)

### Example 1: Hide Settings from Non-Admins
```typescript
import { PermissionGuard } from './components/PermissionGuard';

<PermissionGuard permission="SYSTEM_CONFIG">
  <Settings />
</PermissionGuard>
```

### Example 2: Show Button Only to Admins
```typescript
<PermissionGuard permission="SYSTEM_ADMIN" fallback={<span>Access Denied</span>}>
  <button onClick={handleAdminAction}>Admin Action</button>
</PermissionGuard>
```

### Example 3: Loan Feature for Branch Managers or Admins
```typescript
<PermissionGuard permission={['LOAN_APPROVE', 'SYSTEM_ADMIN']}>
  <LoanApprovalPanel />
</PermissionGuard>
```

### Example 4: Feature Requiring Multiple Permissions
```typescript
<PermissionGuard 
  permission={['SYSTEM_CONFIG', 'AUDIT_READ']} 
  requireAll={true}
>
  <AdvancedSecuritySettings />
</PermissionGuard>
```

### Example 5: Using Hook for Conditional Logic
```typescript
import { useBankingSystem } from './hooks/useBankingSystem';

export function MyComponent() {
  const { hasPermission } = useBankingSystem();
  
  if (hasPermission('SYSTEM_ADMIN')) {
    return <AdminPanel />;
  }
  
  if (hasPermission('LOAN_APPROVE')) {
    return <BranchManagerPanel />;
  }
  
  return <DefaultPanel />;
}
```

---

## Common Permissions Reference

| Permission | Role | Meaning |
|---|---|---|
| `SYSTEM_ADMIN` | Admin | Full system access (bypass all checks) |
| `SYSTEM_CONFIG` | Admin | Configure system settings |
| `ACCOUNT_READ` | All | View customer accounts |
| `ACCOUNT_WRITE` | Manager+ | Modify accounts |
| `TELLER_POST` | Teller | Post deposits/withdrawals |
| `POST_TRANSACTION` | Teller+ | Create transactions |
| `LOAN_READ` | All | View loan information |
| `LOAN_APPROVE` | Branch Manager+ | Approve loans |
| `REPORT_VIEW` | Analyst+ | View reports |
| `REPORT_GENERATE` | Manager+ | Generate custom reports |
| `MANAGE_USERS` | Admin | Create/edit user accounts |
| `MANAGE_ROLES` | Admin | Configure roles and permissions |

---

## Permission Checking Patterns

### Pattern: Admin-Only Feature
```typescript
<PermissionGuard permission="SYSTEM_ADMIN">
  <UserManagementPanel />
</PermissionGuard>
```

### Pattern: Multi-Role Access (OR Logic)
```typescript
<PermissionGuard permission={['LOAN_APPROVE', 'SYSTEM_ADMIN']}>
  <ApprovalQueue />
</PermissionGuard>
```

### Pattern: Role-Specific Content
```typescript
const { getRoleFromToken } = useBankingSystem();
const role = getRoleFromToken();

switch(role) {
  case 'ROL0001': // Admin
    return <AdminDashboard />;
  case 'ROL0002': // Teller
    return <TellerDashboard />;
  default:
    return <DefaultDashboard />;
}
```

### Pattern: Feature Preview (Admin Override)
```typescript
<PermissionGuard 
  permission={['FEATURE_BETA']} 
  fallback={
    <PermissionGuard permission="SYSTEM_ADMIN">
      <BetaFeatureWarning />
    </PermissionGuard>
  }
>
  <BetaFeature />
</PermissionGuard>
```

### Pattern: Conditional Buttons
```typescript
const { hasPermission } = useBankingSystem();

<div className="actions">
  <button>View</button>
  {hasPermission('ACCOUNT_WRITE') && (
    <>
      <button>Edit</button>
      <button>Delete</button>
    </>
  )}
</div>
```

---

## Integration Checklist

When integrating PermissionGuard into a component:

- [ ] Identify the minimum permission required
- [ ] Check [ROLE-PERMISSIONS-REPORT.md](ROLE-PERMISSIONS-REPORT.md) for correct permission names
- [ ] Wrap component with `<PermissionGuard permission="PERMISSION_NAME">`
- [ ] Test with Admin role (should see feature)
- [ ] Test with limited role (should not see feature)
- [ ] Add appropriate fallback UI if needed
- [ ] Verify in browser console no errors appear
- [ ] Check backend API also enforces permission (HTTP 403)

---

## Local Testing

### Get Token for Testing
Open browser Console (F12) and run:
```javascript
const token = localStorage.getItem('bankinsight_token');
console.log('Token:', token);

// Decode to see permissions
const parts = token.split('.');
const payload = JSON.parse(atob(parts[1]));
console.log('Permissions:', payload.permissions);
console.log('Role:', payload.role_name);
```

### Test Permission Logic
```javascript
// Import jwtUtils in React component
import { hasPermission } from './lib/jwtUtils';

const token = localStorage.getItem('bankinsight_token');
console.log('Has SYSTEM_ADMIN:', hasPermission(token, 'SYSTEM_ADMIN'));
console.log('Has TELLER_POST:', hasPermission(token, 'TELLER_POST'));
console.log('All permissions:', getPermissionsFromToken(token));
```

### Switch User Roles
1. Open DevTools → Application → localStorage
2. Copy current token
3. Logout and login as different user
4. New token will reflect new permissions
5. Refresh page to reload with new permissions

---

## TypeScript Types

### PermissionGuardProps
```typescript
interface PermissionGuardProps {
  permission: string | string[];           // Single or array of permissions
  requireAll?: boolean;                     // false = OR logic (default), true = AND logic
  fallback?: React.ReactNode;               // UI when permission denied
  token?: string;                           // Optional; uses localStorage if not provided
  children: React.ReactNode;                // Content to show if allowed
}
```

### JWTPayload
```typescript
interface JWTPayload {
  sub: string;                              // User ID
  email: string;                            // User email
  role_id: string;                          // Role ID
  role_name: string;                        // Human-readable role name
  permissions: string[];                    // Array of permission strings
  exp: number;                              // Expiration timestamp
  iat: number;                              // Issued at timestamp
  branch_id?: string;                       // Optional: user's branch
}
```

---

## Troubleshooting Guide

### Problem: Button Still Visible After Permission Denial
```typescript
// ❌ Wrong - hasPermission function not called
<button style={{ display: isAdmin ? 'block' : 'none' }}>
  Admin Button
</button>

// ✅ Correct - using PermissionGuard
<PermissionGuard permission="SYSTEM_ADMIN">
  <button>Admin Button</button>
</PermissionGuard>
```

### Problem: "Token not found" in Console
```typescript
// ❌ Wrong - no token provided
<PermissionGuard permission="ADMIN">
  <AdminPanel />
</PermissionGuard>

// ✅ Correct - token provided
const token = localStorage.getItem('bankinsight_token');
<PermissionGuard permission="ADMIN" token={token}>
  <AdminPanel />
</PermissionGuard>

// Or just rely on auto-detection from localStorage
```

### Problem: Permission Check Not Working
1. Check token exists: `localStorage.getItem('bankinsight_token')`
2. Verify permission name (case-sensitive): Check [ROLE-PERMISSIONS-REPORT.md](ROLE-PERMISSIONS-REPORT.md)
3. Decode token and check permissions array: `JSON.parse(atob(token.split('.')[1]))`
4. Verify backend also enforces (test API with curl/Postman, expect HTTP 403)

### Problem: Sidebar Items Still Showing
- Verify `useBankingSystem` imports from `jwtUtils`
- Check `hasPermission()` returns correct value
- Restart dev server (Vite might cache)
- Clear browser cache and localStorage

---

## Backend Integration

⚠️ **Critical**: Permission checks on frontend are **NOT** a security boundary!

### Backend Must Also Enforce
```csharp
// BankInsight.API/Controllers/SomeController.cs
[HttpGet("admin-only")]
[RequirePermission("SYSTEM_ADMIN")]
public ActionResult AdminOnly() {
    return Ok("Admin content");
}

// Returns HTTP 403 Forbidden if user lacks permission
```

### Expected Behavior
- **Frontend**: PermissionGuard hides component (UX)
- **Backend**: [RequirePermission] returns 403 (security)

Both layers are needed for security!

---

## Performance Considerations

### Memoization Example
```typescript
const { token } = useBankingSystem();

// Recalculated on every render
const isAdmin = hasPermission(token, 'SYSTEM_ADMIN');  // ❌ Inefficient

// Memoized, only recalculated if token changes
const isAdmin = useMemo(
  () => hasPermission(token, 'SYSTEM_ADMIN'),
  [token]
);  // ✅ Efficient
```

### Avoiding Over-Wrapping
```typescript
// ❌ Excessive wrapping
<PermissionGuard permission="READ_A">
  <PermissionGuard permission="READ_B">
    <PermissionGuard permission="READ_C">
      <Component />
    </PermissionGuard>
  </PermissionGuard>
</PermissionGuard>

// ✅ Efficient - single wrapper with array
<PermissionGuard 
  permission={['READ_A', 'READ_B', 'READ_C']}
  requireAll={true}
>
  <Component />
</PermissionGuard>
```

---

## Real-World Examples

### Example: User Management Page
```typescript
import { PermissionGuard } from './components/PermissionGuard';
import UserList from './components/UserList';
import UserForm from './components/UserForm';

export function UserManagement() {
  return (
    <div>
      <h1>User Management</h1>
      
      {/* View users - anyone with MANAGE_USERS */}
      <PermissionGuard permission="MANAGE_USERS">
        <UserList />
      </PermissionGuard>
      
      {/* Create users - admin only */}
      <PermissionGuard 
        permission="SYSTEM_ADMIN"
        fallback={<p>Admin access required</p>}
      >
        <UserForm />
      </PermissionGuard>
    </div>
  );
}
```

### Example: Dashboard Routing
```typescript
import { useBankingSystem } from './hooks/useBankingSystem';
import AdminDashboard from './components/AdminDashboard';
import TellerDashboard from './components/TellerDashboard';
import DefaultDashboard from './components/DefaultDashboard';

export function Dashboard() {
  const { hasPermission } = useBankingSystem();
  
  if (hasPermission('SYSTEM_ADMIN')) {
    return <AdminDashboard />;
  }
  
  if (hasPermission('TELLER_POST')) {
    return <TellerDashboard />;
  }
  
  return <DefaultDashboard />;
}
```

### Example: Settings with Role-Based Sections
```typescript
import { PermissionGuard } from './components/PermissionGuard';

export function Settings() {
  return (
    <div>
      <h2>Settings</h2>
      
      <PermissionGuard permission="ACCOUNT_WRITE">
        <section>
          <h3>Account Settings</h3>
          {/* Account settings UI */}
        </section>
      </PermissionGuard>
      
      <PermissionGuard permission="SYSTEM_CONFIG">
        <section>
          <h3>System Configuration</h3>
          <h4>Database Settings</h4>
          {/* DB config UI */}
          <h4>Security Settings</h4>
          {/* Security config UI */}
        </section>
      </PermissionGuard>
    </div>
  );
}
```

---

## Deployment Checklist

- [ ] All components compile without errors
- [ ] No TypeScript errors for permission usage
- [ ] Backend APIs enforce permissions (HTTP 403)
- [ ] Test each role has correct sidebar menu
- [ ] Test restricted components show fallback UI
- [ ] Console shows no permission-related errors
- [ ] Audit logs record permission denials
- [ ] User UAT confirms correct feature visibility
- [ ] Performance baseline for permission checks

---

## Support & Resources

- **Full Documentation**: [PERMISSION-AWARE-UI-GUIDE.md](PERMISSION-AWARE-UI-GUIDE.md)
- **Permission List**: [ROLE-PERMISSIONS-REPORT.md](ROLE-PERMISSIONS-REPORT.md)
- **Test Suite**: `permission-aware-ui-test.ps1`
- **Status Report**: [PERMISSION-AWARE-UI-STATUS.md](PERMISSION-AWARE-UI-STATUS.md)

---

## Questions?

Refer to these sections:
- **"How do I hide a component for non-admins?"** → See Pattern: Admin-Only Feature
- **"What's the permission name for X feature?"** → Check ROLE-PERMISSIONS-REPORT.md
- **"Why is my component still showing?"** → See Troubleshooting section
- **"How do I test locally?"** → See Local Testing section
- **"What's the Teller permission?"** → `TELLER_POST` or `POST_TRANSACTION`

---

**Last Updated**: March 3, 2026  
**Version**: 1.0 - Permission-Aware UI System Complete
