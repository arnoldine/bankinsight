$loanContent = Get-Content Controllers\LoanController.cs
$loanContent = $loanContent -replace '\[RequirePermission\("VIEW_LOANS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Loans.View)]'
$loanContent = $loanContent -replace '\[RequirePermission\("DISBURSE_LOANS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Loans.Disburse)]'
$loanContent = $loanContent -replace '\[RequirePermission\("LOAN_APPROVE"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Loans.Approve)]'
$loanContent = $loanContent -replace '\[RequirePermission\("REPAY_LOANS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]'
$loanContent = $loanContent -replace '\[RequirePermission\("MANAGE_PRODUCTS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Roles.Manage)]'
$loanContent = $loanContent -replace '\[RequirePermission\("MANAGE_GL"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.GeneralLedger.Manage)]'
$loanContent = $loanContent -replace '\[RequirePermission\("MANAGE_LOANS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Loans.Edit)]'
$loanContent = $loanContent -replace '\[RequirePermission\("AUDIT_READ"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Audit.View)]'
$loanContent = $loanContent -replace '\[RequirePermission\("REPORT_VIEW"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Reports.View)]'
$loanContent | Set-Content Controllers\LoanController.cs

$txnContent = Get-Content Controllers\TransactionController.cs
$txnContent = $txnContent -replace '\[RequirePermission\("VIEW_TRANSACTIONS"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Transactions.View)]'
$txnContent = $txnContent -replace '\[RequirePermission\("POST_TRANSACTION"\)]', '[HasPermission(BankInsight.API.Security.AppPermissions.Transactions.Post)]'
$txnContent | Set-Content Controllers\TransactionController.cs
