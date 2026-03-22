# BankInsight Role-Based Workspaces - Quick Reference

## How to Access Workspaces

After logging in, look for the **"Workspaces"** section in the left sidebar (between Navigation and System sections).

## Available Workspaces

### 🎯 Loan Officer Workspace
**Who can access**: Users with `LOAN_READ` permission  
**Icon**: ClipboardList  
**Location**: Sidebar → Workspaces → Loan Officer

**What you can do**:
- View your loan pipeline (pending applications)
- Track your active loan portfolio
- Create new loan appraisals
- Manage customer follow-ups

**Tabs**:
1. **Pipeline** - See all pending loan applications
2. **Portfolio** - Monitor active loans with PAR buckets
3. **Appraisal** - Submit new loan applications
4. **Follow-up** - Track customers needing attention

---

### 🧮 Accountant Workspace
**Who can access**: Users with `GL_POST` permission  
**Icon**: Calculator  
**Location**: Sidebar → Workspaces → Accountant

**What you can do**:
- Post journal entries to the General Ledger
- Reconcile GL accounts
- Generate financial reports
- Close accounting periods

**Tabs**:
1. **Journal Entry** - Post manual GL entries (auto-checks balance)
2. **Reconciliation** - Reconcile accounts one by one
3. **Financial Reports** - Access trial balance, income statement, balance sheet
4. **Period Closing** - Close month/year (with safety checks)

**Important**: Journal entries must be balanced (Total Debits = Total Credits) before posting!

---

### 🎧 Customer Service Workspace
**Who can access**: Users with `ACCOUNT_READ` permission  
**Icon**: Headphones  
**Location**: Sidebar → Workspaces → Customer Service

**What you can do**:
- Look up customer information by CIF
- View customer accounts and transactions
- Create and manage support tickets
- Track service history

**Tabs**:
1. **Customer Lookup** - Search by CIF, view full customer profile
2. **Support Tickets** - See all open tickets
3. **Create Ticket** - File new support request (must select customer first)
4. **Service History** - View resolved tickets

**Workflow**: Always search for customer first, then create ticket from the Create Ticket tab.

---

### 🛡️ Compliance Officer Workspace
**Who can access**: Users with `AUDIT_READ` permission  
**Icon**: Shield  
**Location**: Sidebar → Workspaces → Compliance

**What you can do**:
- Verify customer KYC documents
- Monitor suspicious transactions (AML)
- Screen customers against sanctions lists
- Generate compliance reports

**Tabs**:
1. **KYC Verification** - Review pending KYC, approve/reject
2. **AML Monitoring** - Review flagged transactions, file SARs
3. **Sanctions Screening** - Search customers against watchlists
4. **Compliance Reports** - Access SAR, CTR, KYC summaries

**Actions**:
- **KYC**: Select customer → Review documents → Approve/Reject/Request Info
- **AML**: Review flagged item → Actions (File SAR, Block, Clear Flag)

---

## Quick Tips

### For Loan Officers:
- Use the **Pipeline** tab to prioritize approval workflow
- Sort **Portfolio** by PAR bucket to identify at-risk loans
- **Follow-up** tab shows customers ordered by priority

### For Accountants:
- The system prevents posting unbalanced journal entries
- Period closing is blocked if trial balance is out of balance
- Use reconciliation to match GL accounts with bank records

### For Customer Service:
- Always search customer first before creating a ticket
- Priority levels: LOW, MEDIUM, HIGH, URGENT
- Recent transactions show last 10 only

### For Compliance:
- Pending KYC customers require immediate attention
- Flagged transactions include risk levels (HIGH, MEDIUM, LOW)
- Sanctions screening searches by CIF or name

---

## Dashboard Metrics

Each workspace has 4 metric cards at the top showing real-time statistics:

| Workspace | Metric 1 | Metric 2 | Metric 3 | Metric 4 |
|-----------|----------|----------|----------|----------|
| Loan Officer | Active Loans | Pending Approvals | Avg Loan Size | Collection Rate |
| Accountant | Posted Today | Pending Recons | Total Debits | Total Credits |
| Customer Service | Open Tickets | Resolved Today | Avg Response Time | Satisfaction Score |
| Compliance | Pending KYC | Flagged Transactions | High-Risk Clients | Compliance Score |

---

## Common Questions

**Q: I don't see a workspace in my sidebar**  
A: You need the appropriate permission. Contact your administrator to grant the required permission:
- Loan Officer → `LOAN_READ`
- Accountant → `GL_POST`
- Customer Service → `ACCOUNT_READ`
- Compliance → `AUDIT_READ`

**Q: Can I access multiple workspaces?**  
A: Yes! If your user has multiple permissions, you'll see all applicable workspaces in the sidebar.

**Q: What happens if I try to submit an unbalanced journal entry?**  
A: The system will show an "Out of Balance" warning and disable the submit button until debits equal credits.

**Q: Can I create a support ticket without selecting a customer?**  
A: No, you must first search and select a customer from the Lookup tab, then switch to Create Ticket tab.

**Q: What's the difference between KYC approval and rejection?**  
A: 
- **Approved** - Customer passes verification, can proceed with banking services
- **Rejected** - Customer fails verification, account may be restricted
- **Pending Info** - Request additional documents from customer

---

## Keyboard Shortcuts

- **Enter** in search fields → Execute search
- **Tab** → Navigate form fields
- **Esc** → Close modals (future feature)

---

## Support

For technical issues with workspaces:
1. Check console for errors (F12 → Console tab)
2. Verify you're logged in with the correct role
3. Contact system administrator if problems persist

For business process questions:
- Loan processes → Loan Manager
- Accounting procedures → Head of Finance
- Customer service policies → Customer Service Manager
- Compliance rules → Compliance Officer
