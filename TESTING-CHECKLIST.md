# Role-Based Workspaces Testing Checklist

**Date**: March 5, 2026  
**Frontend Status**: ✅ Running on http://localhost:3000  
**Tester**: You!

---

## Quick Access

### Frontend URL
```
http://localhost:3000
```

### Test Credentials

| Role | Email | Password | Expected Workspaces |
|------|-------|----------|-------------------|
| **Admin** | admin@bankinsight.local | password123 | All 4 ✅✅✅✅ |
| **Manager** | manager@bankinsight.local | password123 | 2 only ✅✅ |
| **Teller** | teller@bankinsight.local | password123 | 1 only ✅ |

---

## 🔐 PRIVILEGE TESTING - All Users with Leases

This section tests the complete privilege lease system across all user types.

### Part A: Admin User - Full Privilege Management

#### Step A1: Login as Admin
- [ ] Clear browser cache and go to http://localhost:3000
- [ ] Click "Login with credentials"
- [ ] Email: `admin@bankinsight.local`
- [ ] Password: `password123`
- [ ] Click "Sign In"
- [ ] Expected: Dashboard loads with all 4 workspaces visible

**Result**: ___________________________________

#### Step A2: Check Initial Permissions (Admin)
- [ ] Navigate to Settings tab (gear icon ⚙️ in left sidebar)
- [ ] Look at the top nav bar (right side, after branch info)
- [ ] Note: Is there a leased permissions badge? (Should be NO initially)
- [ ] Click "USERS" tab in Settings
- [ ] Verify admin has these permissions in the list:
  - [ ] MANAGE_USERS
  - [ ] MANAGE_ROLES
  - [ ] VIEW_CONFIG
  - [ ] MANAGE_ROLES ← (required to create leases)

**Result**: ___________________________________

#### Step A3: Create a Lease for Manager User
- [ ] In Settings, click "LEASES" tab
- [ ] In "Staff" dropdown, select: **Manager User** (manager@bankinsight.local)
- [ ] In "Permission" dropdown, select: **MANAGE_USERS**
- [ ] In "Reason" field, type: `Temporary access for user onboarding`
- [ ] In "Duration (hours)" field, enter: `3`
- [ ] Click "Create Lease" button
- [ ] Expected: Success message + lease appears in table below

**Result**: ✅ PASS / ❌ FAIL

Lease Details Captured:
- Staff: ___________________________________
- Permission: ___________________________________
- Expires: ___________________________________
- Lease ID: ___________________________________

#### Step A4: Create a Lease for Teller User  
- [ ] In "Staff" dropdown, select: **Teller User** (teller@bankinsight.local)
- [ ] In "Permission" dropdown, select: **VIEW_CONFIG**
- [ ] In "Reason" field, type: `Configuration audit access`
- [ ] In "Duration (hours)" field, enter: `2`
- [ ] Click "Create Lease" button
- [ ] Expected: Second lease appears in the Active Leases table
- [ ] Verify table now shows 2 leases total

**Result**: ✅ PASS / ❌ FAIL

#### Step A5: Verify Admin's Own Leases
- [ ] In "Staff" dropdown, select: **Admin User** (admin@bankinsight.local)  
- [ ] In "Permission" dropdown, select: **AUDIT_LOGS**
- [ ] In "Reason" field, type: `Self-granted audit privilege`
- [ ] In "Duration (hours)" field, enter: `1`
- [ ] Click "Create Lease" button
- [ ] Expected: Lease created for admin
- [ ] Check top navigation bar (right side): 
  - [ ] **CRITICALLY IMPORTANT**: An amber/yellow badge should appear showing "1 Leased Permission"
  - [ ] Hover over badge to see tooltip with permission name "AUDIT_LOGS"
  - [ ] This proves the nav badge works!

**Result**: ✅ Badge Visible / ❌ Badge Missing

---

### Part B: Manager User - Restricted Privileges

#### Step B1: Logout and Login as Manager
- [ ] Click LogOut button (top right) 🚪
- [ ] Confirm logout
- [ ] Go to http://localhost:3000 again
- [ ] Click "Login with credentials"
- [ ] Email: `manager@bankinsight.local`
- [ ] Password: `password123`
- [ ] Click "Sign In"

**Result**: ___________________________________

#### Step B2: Check Manager's Permissions
- [ ] Navigate to Settings tab ⚙️
- [ ] Check top nav bar for badge:
  - [ ] ✅ SHOULD see "1 Leased Permission" badge (the MANAGE_USERS lease we created)
  - [ ] Hover to confirm it says "MANAGE_USERS"
- [ ] Click "USERS" tab
- [ ] View Manager's default permissions:
  - [ ] MANAGE_USERS (default)
  - [ ] VIEW_REPORTS (default)
  - [ ] Check if MANAGE_USERS shows as both regular AND leased?

**Result**: Badge Status ___________________________________

#### Step B3: Verify Manager CANNOT Create Leases (Permission Denied)
- [ ] In Settings, click "LEASES" tab
- [ ] Try to create a lease for any user:
  - [ ] Select "Admin User" from Staff dropdown
  - [ ] Select "VIEW_CONFIG" from Permission dropdown
  - [ ] Enter reason: `Testing manager restriction`
  - [ ] Duration: `1`
  - [ ] Click "Create Lease" button
- [ ] Expected RESULT: Error message appearing (403 Forbidden or "Permission Denied")
  - [ ] Because Manager does NOT have MANAGE_ROLES permission

**Result**: ✅ Correctly Denied / ❌ Incorrectly Allowed

Error Message: ___________________________________

#### Step B4: View Available Leases (Read-Only)
- [ ] Check if Manager can see the "Active Leases" table
- [ ] Note: Manager should SEE leases but NOT create/revoke them
- [ ] Expected: Revoke button either disabled or not visible

**Result**: ___________________________________

---

### Part C: Teller User - Minimal Privileges

#### Step C1: Logout and Login as Teller
- [ ] Click LogOut 🚪
- [ ] Go to http://localhost:3000
- [ ] Email: `teller@bankinsight.local`
- [ ] Password: `password123`
- [ ] Click "Sign In"

**Result**: ___________________________________

#### Step C2: Check Teller's Permissions + Leases
- [ ] Navigate to Settings ⚙️
- [ ] Check top nav bar:
  - [ ] ✅ SHOULD see "1 Leased Permission" badge (the VIEW_CONFIG lease we created for teller)
  - [ ] Hover to confirm it says "VIEW_CONFIG" 
- [ ] Click "USERS" tab
- [ ] Teller's default permissions should be minimal:
  - [ ] PROCESS_TRANSACTIONS (default)
  - [ ] DEPOSIT (default)
  - [ ] WITHDRAW (default)

**Result**: Leased Permission Visible ___________________________________

#### Step C3: Verify Teller CANNOT Create Leases
- [ ] Click "LEASES" tab
- [ ] Try to create a lease:
  - [ ] Select any staff member
  - [ ] Select any permission
  - [ ] Enter reason
  - [ ] Click "Create Lease"
- [ ] Expected: Error message (403 Forbidden)

**Result**: ✅ Correctly Denied / ❌ Incorrectly Allowed

#### Step C4: Verify Teller's New Capability
- [ ] Now that Teller has the VIEW_CONFIG leased permission:
  - [ ] Check if any new UI elements or features are visible
  - [ ] Try to access configuration (if available in UI)
- [ ] Expected: Teller can now access features that require VIEW_CONFIG

Result: ___________________________________

---

### Part D: Back to Admin - Cleanup and Revocation

#### Step D1: Re-Login as Admin
- [ ] Logout as Teller
- [ ] Login again as: `admin@bankinsight.local` / `password123`

**Result**: ___________________________________

#### Step D2: Revoke Leases
- [ ] Go to Settings → LEASES tab
- [ ] You should see 3 active leases:
  1. Manager - MANAGE_USERS
  2. Teller - VIEW_CONFIG
  3. Admin - AUDIT_LOGS

- [ ] Click "Revoke" button on **AUDIT_LOGS** lease (the one we granted to admin)
- [ ] Expected: 
  - [ ] Lease disappears from table
  - [ ] "1 Leased Permission" badge in top nav disappears
- [ ] Confirm other 2 leases still show

**Result**: ✅ PASS / ❌ FAIL

#### Step D3: Revoke Manager's Lease
- [ ] Click "Revoke" on **MANAGE_USERS** lease for Manager
- [ ] Expected: Lease removed
- [ ] Table now shows only 1 lease (Teller's VIEW_CONFIG)

**Result**: ✅ PASS / ❌ FAIL

#### Step D4: Final Cleanup
- [ ] Revoke the last lease (Teller - VIEW_CONFIG)
- [ ] Expected: Active Leases table is now empty
- [ ] No leases shown

**Result**: ✅ PASS / ❌ FAIL

---

### Summary: Privilege Testing Results

| Test | Status | Notes |
|------|--------|-------|
| Admin can create leases | ✅/❌ | _______________ |
| Admin nav badge works | ✅/❌ | _______________ |
| Manager cannot create leases | ✅/❌ | _______________ |
| Manager sees own leases | ✅/❌ | _______________ |
| Teller cannot create leases | ✅/❌ | _______________ |
| Teller sees own leases | ✅/❌ | _______________ |
| Lease revocation works | ✅/❌ | _______________ |
| Permission enforcement | ✅/❌ | _______________ |

---

## Test Execution Checklist

### Step 1: Admin Full Access Test 👤

#### Login
- [ ] Go to http://localhost:3000
- [ ] Click "Login with credentials"
- [ ] Enter: `admin@bankinsight.local`
- [ ] Enter: `password123`
- [ ] Click "Sign In"

#### Sidebar Inspection
- [ ] Look for section labeled "WORKSPACES" in left sidebar
- [ ] Count visible workspace items (should be 4)
- [ ] Check all have correct icons:
  - [ ] 🎯 Loan Officer
  - [ ] 🧮 Accountant
  - [ ] 👥 Customer Service
  - [ ] 🛡️ Compliance

Observation: ___________________________________

#### Test Each Workspace

**Loan Officer** 🎯
- [ ] Click on "Loan Officer" in sidebar
- [ ] Wait for page to load
- [ ] Check header shows "Loan Officer Workspace"
- [ ] Verify 4 metric cards display at top
- [ ] Count tabs (should be 4):
  - [ ] Pipeline (active by default)
  - [ ] Portfolio
  - [ ] Appraisal
  - [ ] Follow-up
- [ ] Click each tab - content should change instantly
- [ ] No errors in console (F12 → Console)

Observation: ___________________________________

**Accountant** 🧮
- [ ] Click on "Accountant" in sidebar
- [ ] Check header shows "Accountant Workspace"
- [ ] Verify 4 metric cards display
- [ ] Count tabs (should be 4):
  - [ ] Journal Entry (active by default)
  - [ ] Reconciliation
  - [ ] Financial Reports
  - [ ] Period Closing
- [ ] Click each tab - content should change
- [ ] Verify Journal Entry form shows:
  - [ ] Date field
  - [ ] Reference field
  - [ ] Description field
  - [ ] Multiple line items with Account/Debit/Credit columns
  - [ ] Balance status indicator (Balanced/Out of Balance)

Observation: ___________________________________

**Customer Service** 👥
- [ ] Click on "Customer Service" in sidebar
- [ ] Check header shows "Customer Service"
- [ ] Verify 4 metric cards display
- [ ] Count tabs (should be 4):
  - [ ] Customer Lookup (active by default)
  - [ ] Support Tickets
  - [ ] Create Ticket
  - [ ] Service History
- [ ] Click each tab - content should change
- [ ] Verify Lookup tab shows search form

Observation: ___________________________________

**Compliance** 🛡️
- [ ] Click on "Compliance" in sidebar
- [ ] Check header shows "Compliance Officer"
- [ ] Verify 4 metric cards display
- [ ] Verify compliance score bar displays (0-100%)
- [ ] Count tabs (should be 4):
  - [ ] KYC Verification (active by default)
  - [ ] AML Monitoring
  - [ ] Sanctions Screening
  - [ ] Compliance Reports
- [ ] Click each tab - content should change

Observation: ___________________________________

---

### Step 2: Manager Limited Access Test 👤

#### Logout
- [ ] Click your profile icon (top right)
- [ ] Click "Logout"
- [ ] Verify you're back at login screen

#### Login as Manager
- [ ] Enter: `manager@bankinsight.local`
- [ ] Enter: `password123`
- [ ] Click "Sign In"

#### Sidebar Inspection - CRITICAL TEST
- [ ] Find "WORKSPACES" section in sidebar
- [ ] Count visible items (should be EXACTLY 2):
  - [ ] ✅ 🎯 Loan Officer (VISIBLE)
  - [ ] ❌ 🧮 Accountant (NOT VISIBLE)
  - [ ] ✅ 👥 Customer Service (VISIBLE)
  - [ ] ❌ 🛡️ Compliance (NOT VISIBLE)

**Expected Result**: Exactly 2 workspaces visible

Observation: ___________________________________

#### Test Visible Workspaces
- [ ] Click Loan Officer → Loads successfully
- [ ] Click Customer Service → Loads successfully

**Expected Result**: Both workspaces work properly

---

### Step 3: Teller Minimal Access Test 👤

#### Logout
- [ ] Click your profile icon (top right)
- [ ] Click "Logout"

#### Login as Teller
- [ ] Enter: `teller@bankinsight.local`
- [ ] Enter: `password123`
- [ ] Click "Sign In"

#### Sidebar Inspection - CRITICAL TEST
- [ ] Find "WORKSPACES" section in sidebar
- [ ] Count visible items (should be EXACTLY 1):
  - [ ] ❌ 🎯 Loan Officer (NOT VISIBLE)
  - [ ] ❌ 🧮 Accountant (NOT VISIBLE)
  - [ ] ✅ 👥 Customer Service (VISIBLE - ONLY THIS)
  - [ ] ❌ 🛡️ Compliance (NOT VISIBLE)

**Expected Result**: Exactly 1 workspace visible

Observation: ___________________________________

#### Test Customer Service
- [ ] Click Customer Service → Loads successfully

**Expected Result**: Customer Service workspace works

---

## Form Validation Tests (Admin User)

### Journal Entry Form (Accountant Workspace)

Return to Admin login if not already:
- [ ] Login as admin@bankinsight.local / password123
- [ ] Go to Accountant workspace
- [ ] Click Journal Entry tab

#### Balance Validation Test
- [ ] Fill in Date field
- [ ] Fill in Reference field (e.g., "JE-001")
- [ ] Fill in Description field
- [ ] Add two line items:
  - Line 1: Select an account, enter Debit amount
  - Line 2: Select an account, enter Credit amount
- [ ] Amounts are NOT equal (e.g., Debit 100, Credit 90)
- [ ] Check balance indicator: Should show "Out of Balance" ❌
- [ ] Check submit button: Should be DISABLED (grayed out)
- [ ] Now make amounts equal (both 100)
- [ ] Check balance indicator: Should show "Balanced" ✅
- [ ] Check submit button: Should be ENABLED (clickable)

**Expected Result**: Form prevents unbalanced submissions

Observation: ___________________________________

### Customer Lookup Form (Customer Service Workspace)

- [ ] Go to Customer Service workspace
- [ ] Click Customer Lookup tab
- [ ] Look for CIF search field
- [ ] Enter a customer CIF (if data available)
- [ ] Click Search button
- [ ] Verify results display (or "not found" message)

**Expected Result**: Search form works without errors

Observation: ___________________________________

---

## Privilege Lease Management Test

This feature allows admins to grant temporary elevated permissions to staff members.

### Setup: Login as Admin
- [ ] Login as `admin@bankinsight.local` / `password123`
- [ ] Navigate to Settings tab (gear icon in sidebar)
- [ ] Click on "LEASES" tab

### Test 1: Create a Privilege Lease
- [ ] From the Staff dropdown, select a user (e.g., "Manager User")
- [ ] From the Permission dropdown, select a permission (e.g., "VIEW_CONFIG")
- [ ] Enter a reason (e.g., "Temporary access for testing")
- [ ] Set duration in hours (e.g., 2)
- [ ] Click "Create Lease" button
- [ ] Verify success message appears
- [ ] Verify the new lease appears in the "Active Leases" table below

**Expected Result**: Lease is created and listed

Observation: ___________________________________

### Test 2: View Active Leases
- [ ] Check the Active Leases table
- [ ] Verify it shows:
  - [ ] Staff member name
  - [ ] Permission granted
  - [ ] Reason for lease
  - [ ] Approved by (should be current admin)
  - [ ] Expiry date/time
  - [ ] Revoke button

**Expected Result**: All lease details are visible

Observation: ___________________________________

### Test 3: Revoke a Privilege Lease
- [ ] Click the "Revoke" button on the lease you just created
- [ ] Verify the lease disappears from the Active Leases table
- [ ] (Or verify it's marked as revoked if the UI shows revoked leases)

**Expected Result**: Lease is successfully revoked

Observation: ___________________________________

### Test 4: Nav Badge for Current User
This requires creating a lease for the currently logged-in admin user.

- [ ] Create a new lease for the admin user (yourself)
- [ ] Select admin user from staff dropdown
- [ ] Choose any permission (e.g., "MANAGE_USERS")
- [ ] Enter reason and duration
- [ ] Click "Create Lease"
- [ ] Look at the top navigation bar (next to branch badge)
- [ ] Verify an amber/yellow badge appears showing "1 Leased Permission"
- [ ] Hover over the badge to see tooltip with permission name
- [ ] Go back to LEASES tab and revoke the lease
- [ ] Verify the badge disappears from the top nav

**Expected Result**: Badge appears when leases are active, disappears when revoked

Observation: ___________________________________

### Test 5: Refresh Leases
- [ ] Create a new lease for any staff member
- [ ] Click the "Refresh" button (🔄) above the Active Leases table
- [ ] Verify the table updates with current leases

**Expected Result**: Refresh button updates the lease list

Observation: ___________________________________

### Test 6: Multiple Leases
- [ ] Create 2-3 leases for different staff members with different permissions
- [ ] Verify all appear in the Active Leases table
- [ ] Revoke one lease
- [ ] Verify count updates correctly

**Expected Result**: Multiple leases can be managed simultaneously

Observation: ___________________________________

---

## Responsive Design Test

Skip this if not needed, but useful for completeness:

### Mobile Test
- [ ] Open DevTools (F12)
- [ ] Click device toggle (top left of DevTools)
- [ ] Select "iPhone SE" (375px width)
- [ ] Click on Admin → Loan Officer workspace
- [ ] Verify:
  - [ ] Layout stacks vertically
  - [ ] Metric cards are readable
  - [ ] Tabs are accessible
  - [ ] Forms are usable

**Expected Result**: UI adapts to mobile size

Observation: ___________________________________

### Tablet Test
- [ ] Change DevTools to "iPad" (768px width)
- [ ] Navigate between workspaces
- [ ] Verify:
  - [ ] Layout adjusts appropriately
  - [ ] Grid changes from 4 columns to 2
  - [ ] Content is readable

**Expected Result**: UI adapts to tablet size

Observation: ___________________________________

---

## Console Error Check

### Critical Test
- [ ] Press F12 to open DevTools
- [ ] Go to "Console" tab
- [ ] Navigate through all 4 workspaces (as admin)
- [ ] Click through all tabs in each workspace
- [ ] Check for RED ERROR messages

**Expected Result**: No red errors in console

List any errors found:
- [ ] Error 1: ___________________
- [ ] Error 2: ___________________
- [ ] Error 3: ___________________

---

## Summary of Results

### Permission Visibility
- [ ] Admin sees all 4 workspaces: **PASS / FAIL**
- [ ] Manager sees 2 workspaces: **PASS / FAIL**
- [ ] Teller sees 1 workspace: **PASS / FAIL**

### Workspace Functionality
- [ ] Loan Officer loads correctly: **PASS / FAIL**
- [ ] Accountant loads correctly: **PASS / FAIL**
- [ ] Customer Service loads correctly: **PASS / FAIL**
- [ ] Compliance loads correctly: **PASS / FAIL**

### Tab Navigation
- [ ] All tabs switch content: **PASS / FAIL**
- [ ] Active tab styling works: **PASS / FAIL**

### Form Validation
- [ ] Journal entry balance check works: **PASS / FAIL**
- [ ] Customer search works: **PASS / FAIL**

### Browser Console
- [ ] No critical errors: **PASS / FAIL**

### Responsive Design
- [ ] Mobile view works: **PASS / FAIL**
- [ ] Tablet view works: **PASS / FAIL**

---

## Overall Assessment

### ✅ PASS - All tests completed successfully
### ⚠️ PARTIAL - Some tests failed
### ❌ FAIL - Multiple tests failed

**Overall Status**: ___________________________

**Issues Encountered**:
1. _____________________________________
2. _____________________________________
3. _____________________________________

**Additional Notes**:
_____________________________________
_____________________________________
_____________________________________

---

## Test Completion

**Started**: March 5, 2026  
**Completed**: _______________  
**Tester Name**: _______________  
**Sign-off**: _______________

### Next Steps
- [ ] Report results to development team
- [ ] Document any bugs found
- [ ] Schedule backend integration testing
- [ ] Plan for production deployment

---

## Quick Reference: What To Look For

### ✅ GOOD Signs
- Workspaces appear/disappear based on permissions
- Tab switching is instant with no page reload
- Forms display with proper formatting
- Metric cards show numbers/values
- No red errors in console
- UI adapts to different screen sizes

### ❌ BAD Signs
- Workspace doesn't match user's role
- Code errors in browser console
- Forms are broken/misaligned
- Tabs don't switch content
- Page reloads when clicking tabs
- UI breaks on smaller screens

---

## Contact

If you find issues during testing:
1. Note the exact steps that caused the issue
2. Check browser console for error messages
3. Take screenshots if helpful
4. Report findings to the development team

**Testing Environment Ready! Begin Testing Now:** 🚀

---
