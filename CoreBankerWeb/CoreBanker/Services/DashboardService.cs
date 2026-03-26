namespace CoreBanker.Services
{
    public class DashboardService
    {
        private readonly ClientService _clientService;
        private readonly AccountService _accountService;
        private readonly LoanService _loanService;
        private readonly TransactionService _transactionService;
        private readonly AuditService _auditService;
        private readonly ApprovalService _approvalService;

        public DashboardService(
            ClientService clientService,
            AccountService accountService,
            LoanService loanService,
            TransactionService transactionService,
            AuditService auditService,
            ApprovalService approvalService)
        {
            _clientService = clientService;
            _accountService = accountService;
            _loanService = loanService;
            _transactionService = transactionService;
            _auditService = auditService;
            _approvalService = approvalService;
        }

        public async Task<DashboardSnapshot> GetDashboardSnapshotAsync(CancellationToken cancellationToken = default)
        {
            var clientsTask = _clientService.GetClientsAsync(cancellationToken);
            var accountsTask = _accountService.GetAccountsAsync(cancellationToken);
            var loansTask = _loanService.GetLoansAsync();
            var transactionsTask = _transactionService.GetTransactionsAsync(cancellationToken);
            var auditsTask = _auditService.GetAuditLogsAsync(50, cancellationToken);
            var approvalsTask = _approvalService.GetApprovalsAsync();

            await Task.WhenAll(clientsTask, accountsTask, loansTask, transactionsTask, auditsTask, approvalsTask);

            var clients = clientsTask.Result;
            var accounts = accountsTask.Result;
            var loans = loansTask.Result;
            var transactions = transactionsTask.Result;
            var audits = auditsTask.Result;
            var approvals = approvalsTask.Result;

            var today = DateTime.UtcNow.Date;
            var todaysTransactions = transactions.Where(transaction => transaction.Date.Date == today).ToList();

            return new DashboardSnapshot
            {
                Kpis = new DashboardKpiData
                {
                    ActiveClients = clients.Count(client => string.Equals(client.Status, "Active", StringComparison.OrdinalIgnoreCase)),
                    ActiveAccounts = accounts.Count(account => string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase)),
                    ActiveLoans = loans.Count(loan => string.Equals(loan.Status, "Active", StringComparison.OrdinalIgnoreCase)),
                    TransactionsToday = todaysTransactions.Count,
                    AuditExceptions = audits.Count(log => log.Status is "FAILURE" or "FAILED"),
                    PendingApprovals = approvals.Count(approval => string.Equals(approval.Status, "Pending", StringComparison.OrdinalIgnoreCase)),
                    TransactionValueToday = todaysTransactions.Sum(transaction => transaction.Amount),
                    TotalDeposits = accounts.Sum(account => account.Balance)
                },
                RecentTransactions = transactions
                    .OrderByDescending(transaction => transaction.Date)
                    .Take(8)
                    .Select(transaction => new RecentTransaction
                    {
                        Id = transaction.Id,
                        AccountId = transaction.AccountId,
                        Type = transaction.Type,
                        Amount = transaction.Amount,
                        Date = transaction.Date,
                        Status = transaction.Status,
                        Reference = transaction.Reference
                    })
                    .ToList(),
                RecentAuditLogs = audits
                    .OrderByDescending(log => log.Date)
                    .Take(6)
                    .ToList()
            };
        }
    }

    public class DashboardSnapshot
    {
        public DashboardKpiData Kpis { get; set; } = new();
        public List<RecentTransaction> RecentTransactions { get; set; } = new();
        public List<AuditLogDto> RecentAuditLogs { get; set; } = new();
    }

    public class DashboardKpiData
    {
        public int ActiveClients { get; set; }
        public int ActiveAccounts { get; set; }
        public int ActiveLoans { get; set; }
        public int TransactionsToday { get; set; }
        public int AuditExceptions { get; set; }
        public int PendingApprovals { get; set; }
        public decimal TransactionValueToday { get; set; }
        public decimal TotalDeposits { get; set; }
    }

    public class RecentTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }
}
