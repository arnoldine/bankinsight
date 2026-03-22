using Microsoft.EntityFrameworkCore;
using BankInsight.API.Entities;

namespace BankInsight.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Branch> Branches { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<GroupMember> GroupMembers { get; set; }
    public DbSet<LendingCenter> LendingCenters { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductGroupRule> ProductGroupRules { get; set; }
    public DbSet<ProductEligibilityRule> ProductEligibilityRules { get; set; }
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Loan> Loans { get; set; }
    public DbSet<LoanProduct> LoanProducts { get; set; }
    public DbSet<LoanAccount> LoanAccounts { get; set; }
    public DbSet<LoanSchedule> LoanSchedules { get; set; }
    public DbSet<LoanRepayment> LoanRepayments { get; set; }
    public DbSet<LoanAccrual> LoanAccruals { get; set; }
    public DbSet<LoanImpairment> LoanImpairments { get; set; }
    public DbSet<LoanAccountingProfile> LoanAccountingProfiles { get; set; }
    public DbSet<LoanDisclosure> LoanDisclosures { get; set; }
    public DbSet<CreditBureauCheck> CreditBureauChecks { get; set; }
    public DbSet<LoanRepaymentBehavior> LoanRepaymentBehaviors { get; set; }
    public DbSet<LoanBogClassification> LoanBogClassifications { get; set; }
    public DbSet<GroupLoanApplication> GroupLoanApplications { get; set; }
    public DbSet<GroupLoanApplicationMember> GroupLoanApplicationMembers { get; set; }
    public DbSet<GroupLoanAccount> GroupLoanAccounts { get; set; }
    public DbSet<GroupMeeting> GroupMeetings { get; set; }
    public DbSet<GroupMeetingAttendance> GroupMeetingAttendances { get; set; }
    public DbSet<GroupCollectionBatch> GroupCollectionBatches { get; set; }
    public DbSet<GroupCollectionBatchLine> GroupCollectionBatchLines { get; set; }
    public DbSet<GroupGuaranteeLink> GroupGuaranteeLinks { get; set; }
    public DbSet<GroupLoanDelinquencySnapshot> GroupLoanDelinquencySnapshots { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<GlAccount> GlAccounts { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalLine> JournalLines { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<NumberSequence> NumberSequences { get; set; }
    public DbSet<Workflow> Workflows { get; set; }
    public DbSet<ApprovalRequest> ApprovalRequests { get; set; }
    public DbSet<PrivilegeLease> PrivilegeLeases { get; set; }
    
    // Process & Workflow Engine
    public DbSet<ProcessDefinition> ProcessDefinitions { get; set; }
    public DbSet<ProcessDefinitionVersion> ProcessDefinitionVersions { get; set; }
    public DbSet<ProcessStepDefinition> ProcessStepDefinitions { get; set; }
    public DbSet<ProcessTransitionDefinition> ProcessTransitionDefinitions { get; set; }
    public DbSet<ProcessInstance> ProcessInstances { get; set; }
    public DbSet<ProcessTask> ProcessTasks { get; set; }
    public DbSet<ProcessInstanceHistory> ProcessInstanceHistories { get; set; }
    public DbSet<ProcessEventSubscription> ProcessEventSubscriptions { get; set; }

    // Event-Driven Architecture
    public DbSet<FinancialEvent> FinancialEvents { get; set; }
    public DbSet<PostingRule> PostingRules { get; set; }
    
    // User Management
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<UserActivity> UserActivities { get; set; }
    
    // Branch Management
    public DbSet<BranchHierarchy> BranchHierarchies { get; set; }
    public DbSet<BranchVault> BranchVaults { get; set; }
    public DbSet<CashIncident> CashIncidents { get; set; }
    public DbSet<BranchLimit> BranchLimits { get; set; }
    public DbSet<InterBranchTransfer> InterBranchTransfers { get; set; }
    public DbSet<BranchConfig> BranchConfigs { get; set; }
    
    // Vault Ledger & Operations
    public DbSet<VaultLedger> VaultLedgers { get; set; }
    public DbSet<VaultTransactionDenomination> VaultTransactionDenominations { get; set; }
    public DbSet<TellerSession> TellerSessions { get; set; }
    
    // Treasury Management
    public DbSet<FxRate> FxRates { get; set; }
    public DbSet<TreasuryPosition> TreasuryPositions { get; set; }
    public DbSet<FxTrade> FxTrades { get; set; }
    public DbSet<Investment> Investments { get; set; }
    public DbSet<RiskMetric> RiskMetrics { get; set; }
    
    // Reporting & Analytics
    public DbSet<ReportDefinition> ReportDefinitions { get; set; }
    public DbSet<ReportParameter> ReportParameters { get; set; }
    public DbSet<ReportSchedule> ReportSchedules { get; set; }
    public DbSet<ReportRun> ReportRuns { get; set; }
    public DbSet<ReportSubscription> ReportSubscriptions { get; set; }
    public DbSet<RegulatoryReturn> RegulatoryReturns { get; set; }
    public DbSet<DataExtract> DataExtracts { get; set; }
    public DbSet<ReportFavorite> ReportFavorites { get; set; }
    public DbSet<ReportFilterPreset> ReportFilterPresets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // RBAC Configuration
        modelBuilder.Entity<RolePermission>()
            .HasKey(rp => new { rp.RoleId, rp.PermissionId });

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId);

        modelBuilder.Entity<RolePermission>()
            .HasOne(rp => rp.Permission)
            .WithMany()
            .HasForeignKey(rp => rp.PermissionId);

        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<Permission>()
            .HasIndex(p => p.Code)
            .IsUnique();

        modelBuilder.Entity<Group>()
            .HasIndex(g => g.GroupCode)
            .IsUnique(false);

        modelBuilder.Entity<GroupMember>()
            .HasIndex(gm => new { gm.GroupId, gm.CustomerId })
            .IsUnique();

        modelBuilder.Entity<GroupMember>()
            .HasOne(gm => gm.Group)
            .WithMany(g => g.Members)
            .HasForeignKey(gm => gm.GroupId);

        modelBuilder.Entity<Group>()
            .HasOne(g => g.Center)
            .WithMany()
            .HasForeignKey(g => g.CenterId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ProductGroupRule>()
            .HasOne(r => r.Product)
            .WithOne(p => p.GroupRule)
            .HasForeignKey<ProductGroupRule>(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductEligibilityRule>()
            .HasOne(r => r.Product)
            .WithOne(p => p.EligibilityRule)
            .HasForeignKey<ProductEligibilityRule>(r => r.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupLoanApplication>()
            .HasOne(a => a.Group)
            .WithMany(g => g.Applications)
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupLoanApplicationMember>()
            .HasOne(m => m.GroupLoanApplication)
            .WithMany(a => a.Members)
            .HasForeignKey(m => m.GroupLoanApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMeeting>()
            .HasOne(m => m.Group)
            .WithMany(g => g.Meetings)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupMeetingAttendance>()
            .HasOne(a => a.GroupMeeting)
            .WithMany(m => m.Attendances)
            .HasForeignKey(a => a.GroupMeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupCollectionBatchLine>()
            .HasOne(l => l.Batch)
            .WithMany(b => b.Lines)
            .HasForeignKey(l => l.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GroupLoanDelinquencySnapshot>()
            .HasIndex(s => new { s.LoanAccountId, s.SnapshotDate });

        modelBuilder.Entity<SystemConfig>()
            .HasIndex(sc => sc.Key)
            .IsUnique();
        
        modelBuilder.Entity<Branch>()
            .HasIndex(b => b.Code)
            .IsUnique();

        modelBuilder.Entity<Staff>()
            .HasIndex(s => s.Email)
            .IsUnique();

        modelBuilder.Entity<PrivilegeLease>()
            .HasIndex(p => new { p.StaffId, p.Permission, p.ExpiresAt });

        modelBuilder.Entity<PrivilegeLease>()
            .Property(p => p.Permission)
            .HasMaxLength(100);

        // Core Banking Event Store
        modelBuilder.Entity<FinancialEvent>()
            .HasIndex(e => new { e.EntityType, e.EntityId });
            
        modelBuilder.Entity<FinancialEvent>()
            .HasIndex(e => e.Reference);
            
        modelBuilder.Entity<FinancialEvent>()
            .HasIndex(e => e.CreatedAt);

        modelBuilder.Entity<PostingRule>()
            .HasIndex(e => e.EventType);

        modelBuilder.Entity<LoanProduct>()
            .HasIndex(lp => lp.Code)
            .IsUnique();

        modelBuilder.Entity<Loan>()
            .HasOne(l => l.LoanProduct)
            .WithMany(lp => lp.Loans)
            .HasForeignKey(l => l.LoanProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LoanRepayment>()
            .HasOne(r => r.Loan)
            .WithMany(l => l.Repayments)
            .HasForeignKey(r => r.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoanAccount>()
            .HasOne(la => la.Loan)
            .WithOne()
            .HasForeignKey<LoanAccount>(la => la.LoanId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<LoanAccount>()
            .HasIndex(la => new { la.BranchId, la.DelinquencyDays });

        modelBuilder.Entity<LoanAccrual>()
            .HasIndex(a => new { a.LoanId, a.AccrualDate })
            .IsUnique();

        modelBuilder.Entity<LoanImpairment>()
            .HasIndex(i => new { i.LoanId, i.CreatedAt });

        modelBuilder.Entity<LoanAccountingProfile>()
            .HasIndex(p => p.LoanProductId)
            .IsUnique();

        modelBuilder.Entity<LoanDisclosure>()
            .HasIndex(d => new { d.LoanId, d.Accepted });

        modelBuilder.Entity<CreditBureauCheck>()
            .HasOne(c => c.Loan)
            .WithMany()
            .HasForeignKey(c => c.LoanId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<CreditBureauCheck>()
            .HasIndex(c => new { c.CustomerId, c.CheckedAt });

        modelBuilder.Entity<CreditBureauCheck>()
            .HasIndex(c => new { c.ProviderName, c.InquiryReference });

        // Process Engine Configuration
        modelBuilder.Entity<ProcessDefinition>()
            .HasIndex(p => p.Code)
            .IsUnique();
        
        modelBuilder.Entity<ProcessDefinitionVersion>()
            .HasIndex(v => new { v.ProcessDefinitionId, v.VersionNo })
            .IsUnique();

        modelBuilder.Entity<ProcessStepDefinition>()
            .HasOne(s => s.ProcessDefinitionVersion)
            .WithMany(v => v.Steps)
            .HasForeignKey(s => s.ProcessDefinitionVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessTransitionDefinition>()
            .HasOne(t => t.ProcessDefinitionVersion)
            .WithMany(v => v.Transitions)
            .HasForeignKey(t => t.ProcessDefinitionVersionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessInstance>()
            .HasOne(i => i.ProcessDefinitionVersion)
            .WithMany()
            .HasForeignKey(i => i.ProcessDefinitionVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProcessTask>()
            .HasOne(t => t.ProcessInstance)
            .WithMany(i => i.Tasks)
            .HasForeignKey(t => t.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProcessInstanceHistory>()
            .HasOne(h => h.ProcessInstance)
            .WithMany(i => i.History)
            .HasForeignKey(h => h.ProcessInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}




