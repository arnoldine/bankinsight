# Test Credentials for BankInsight

## ✅ Status: All Working (Verified March 4, 2026)

All three test accounts have been verified and are working with password `password123`.

## Available Test Users

### 1. Administrator
- **Email:** `admin@bankinsight.local`
- **Password:** `password123`
- **Role:** Administrator
- **Permissions:** Full system access (38 permissions)
- **Use for:** System configuration, user management, all operations
- **Status:** ✅ WORKING

### 2. Teller
- **Email:** `teller@bankinsight.local`
- **Password:** `password123`
- **Role:** Teller
- **Permissions:** (4 permissions)
  - VIEW_ACCOUNTS
  - POST_TRANSACTION
  - VIEW_TRANSACTIONS
  - VIEW_USERS
- **Use for:** Front desk operations, deposits, withdrawals
- **Status:** ✅ WORKING

### 3. Branch Manager
- **Email:** `manager@bankinsight.local`
- **Password:** `password123`
- **Role:** Branch Manager
- **Permissions:** (13 permissions)
  - VIEW_ACCOUNTS
  - CREATE_ACCOUNTS
  - VIEW_TRANSACTIONS
  - POST_TRANSACTION
  - VIEW_LOANS
  - DISBURSE_LOANS
  - VIEW_USERS
  - VIEW_PRODUCTS
  - VIEW_GROUPS
  - CREATE_GROUPS
  - VIEW_GL
  - VIEW_APPROVALS
  - CREATE_APPROVALS
- **Use for:** Branch operations, loan approvals, account management
- **Status:** ✅ WORKING

## Password Policy

- All default passwords are set to `password123`
- On first successful login, the system will automatically convert plaintext passwords to BCrypt hashes
- Users can change their passwords through the Settings > Password Management section

## Adding New Users

New users can be created through:
1. **Settings Panel** (admin access required)
2. **Direct API call** to `POST /api/users`
3. **Database seeder** (modify `BankInsight.API/Data/DatabaseSeeder.cs`)

## Security Notes

⚠️ **Important:** These credentials are for development/testing only. In production:
- Use strong, unique passwords
- Enable password complexity requirements
- Implement password rotation policies
- Consider integrating Azure AD or LDAP authentication
