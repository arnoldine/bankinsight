# BankInsight UI and UX Blueprint

## Design Direction
BankInsight should feel calm, deliberate, and operationally trustworthy. The interface should reduce cognitive load for branch staff, credit teams, operations, finance, and supervisors who spend long hours in the system.

Design principles:
- Task-first, not module-first
- Clear hierarchy and fewer competing actions per screen
- Consistent spacing, status language, and form behavior
- Strong defaults for frequent operations
- Fast scanning for tables, queues, and exception work

## Shell and Navigation
### Goals
- Reduce navigation overload
- Make common workflows reachable in one or two clicks
- Improve orientation so users always know what workspace they are in

### Standards
- Searchable sidebar navigation
- Shortcuts for high-frequency workspaces in the top header
- Consistent page title and active workspace indicator
- Group related features by operational intent, not only technical module

## Design System Standards
### Layout
- Use consistent page padding, card radius, and border treatment.
- Prefer 2-3 clear zones per screen rather than dense all-in-one layouts.
- Keep primary actions visible without burying them in long forms.

### Typography
- Use strong page titles, clear section headers, and subdued helper text.
- Reserve uppercase micro-labels for metadata or compact dashboards only.

### Color and Status
- Use color intentionally for status, risk, warnings, and completion.
- Keep neutral surfaces clean and limit heavy gradients to summary areas.
- Standardize badges for Active, Pending, Approved, Rejected, Suspended, In Arrears, and Closed.

### Forms
- Prefer guided sections over long unbroken forms.
- Use inline validation with plain, operator-friendly error messages.
- Preserve draft state where workflows are long or multi-step.

### Tables and Lists
- Default to sortable, filterable lists with strong empty and loading states.
- Surface summary metrics and actions above long tables.
- Keep row actions consistent across modules.

## Priority UX Modules
### 1. App Shell
- Complete the shell redesign with searchable navigation and workspace shortcuts.
- Add role-sensitive quick links and better empty states.

### 2. Approvals Inbox
- Build a cleaner queue with risk indicators, decision context, and batch triage.
- Surface maker-checker requirements and decision history inline.

### 3. Loan Workspaces
- Separate origination, approval, disbursement, collections, and delinquency views more clearly.
- Improve schedule previews, disclosures, and exception messaging.

### 4. Group Lending
- Keep group setup, applications, meetings, and reports in one coherent workspace.
- Highlight weekly dues, attendance, delinquency, and officer actions.

### 5. Treasury
- Improve position monitoring, trade entry, and investment lifecycle screens.
- Surface risk and exception signals clearly.

### 6. Reporting
- Standardize filters, date ranges, export actions, and result cards.
- Make dashboards easier to scan and compare across branch, officer, and product views.

## Accessibility and Responsiveness
- Ensure keyboard access for core workflows.
- Use readable contrast and visible focus states.
- Support tablet-friendly layouts for field and supervisory use.
- Avoid layouts that collapse into unusable dense tables on smaller screens.

## UX Delivery Sequence
### Phase 1
- Shell and navigation cleanup
- Shared status chips, cards, and action bars
- Better loading, empty, and error states

### Phase 2
- Approvals, loans, and group lending workflow cleanup
- Reporting filter standardization

### Phase 3
- Treasury and operations refinements
- Accessibility pass
- Final responsive adjustments

## Acceptance Signals
- Operators can find core tasks faster with less training.
- High-frequency workflows require fewer clicks and less backtracking.
- Error and exception states explain what happened and what to do next.
- Screens feel visually consistent across modules.
