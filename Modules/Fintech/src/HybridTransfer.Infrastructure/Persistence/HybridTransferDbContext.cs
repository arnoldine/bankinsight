using HybridTransfer.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace HybridTransfer.Infrastructure.Persistence;

public sealed class HybridTransferDbContext : DbContext
{
    public HybridTransferDbContext(DbContextOptions<HybridTransferDbContext> options) : base(options)
    {
    }

    public DbSet<TransferOrderEntity> TransferOrders => Set<TransferOrderEntity>();
    public DbSet<ApprovalRequestEntity> ApprovalRequests => Set<ApprovalRequestEntity>();
    public DbSet<AlertEntity> Alerts => Set<AlertEntity>();
    public DbSet<ReconciliationItemEntity> ReconciliationItems => Set<ReconciliationItemEntity>();
    public DbSet<WebhookReceiptEntity> WebhookReceipts => Set<WebhookReceiptEntity>();
    public DbSet<WalletEntity> Wallets => Set<WalletEntity>();
    public DbSet<JournalEntryEntity> JournalEntries => Set<JournalEntryEntity>();
    public DbSet<JournalLineEntity> JournalLines => Set<JournalLineEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TransferOrderEntity>(entity =>
        {
            entity.ToTable("transfer_orders");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TransferType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Channel).HasMaxLength(32).IsRequired();
            entity.Property(x => x.FundingSource).HasMaxLength(32).IsRequired();
            entity.Property(x => x.DestinationDetails).HasColumnType("jsonb").IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.DestinationCountryCode).HasMaxLength(8).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Fee).HasPrecision(18, 2);
            entity.Property(x => x.FxRate).HasPrecision(18, 8);
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RiskStatus).HasMaxLength(32).IsRequired();
            entity.Property(x => x.ComplianceStatus).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CreatedBy).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(128);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<ApprovalRequestEntity>(entity =>
        {
            entity.ToTable("approval_requests");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActionCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.RequestedBy).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ApprovedBy).HasMaxLength(128);
            entity.Property(x => x.Reason).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.TransferOrderId);
        });

        modelBuilder.Entity<AlertEntity>(entity =>
        {
            entity.ToTable("alerts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.AlertCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Severity).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.CustomerId);
        });

        modelBuilder.Entity<ReconciliationItemEntity>(entity =>
        {
            entity.ToTable("reconciliation_items");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ReconciliationType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.InternalReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(512).IsRequired();
            entity.HasIndex(x => x.Status);
        });

        modelBuilder.Entity<WebhookReceiptEntity>(entity =>
        {
            entity.ToTable("webhook_receipts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ProviderCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProviderReference).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PayloadHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => new { x.ProviderCode, x.ProviderReference, x.PayloadHash }).IsUnique();
        });

        modelBuilder.Entity<WalletEntity>(entity =>
        {
            entity.ToTable("wallets");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.WalletType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.AvailableBalance).HasPrecision(18, 2);
            entity.Property(x => x.ReservedBalance).HasPrecision(18, 2);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => new { x.CustomerId, x.WalletType, x.Currency }).IsUnique();
        });

        modelBuilder.Entity<JournalEntryEntity>(entity =>
        {
            entity.ToTable("journal_entries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Reference).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.SourceModule).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ExternalReference).HasMaxLength(128);
            entity.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasIndex(x => x.IdempotencyKey).IsUnique();
            entity.HasIndex(x => x.ReversalOfJournalEntryId);
            entity.HasIndex(x => x.TransferOrderId);
            entity.HasMany(x => x.Lines).WithOne(x => x.JournalEntry).HasForeignKey(x => x.JournalEntryId);
        });

        modelBuilder.Entity<JournalLineEntity>(entity =>
        {
            entity.ToTable("journal_lines");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Debit).HasPrecision(18, 2);
            entity.Property(x => x.Credit).HasPrecision(18, 2);
            entity.Property(x => x.Currency).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ExchangeRate).HasPrecision(18, 8);
            entity.Property(x => x.Narrative).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.JournalEntryId);
            entity.HasIndex(x => x.LedgerAccountId);
        });

        modelBuilder.Entity<AuditEventEntity>(entity =>
        {
            entity.ToTable("audit_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ActorId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ActorType).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Action).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.EntityId).HasMaxLength(128).IsRequired();
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.DeviceId).HasMaxLength(128);
            entity.Property(x => x.TraceId).HasMaxLength(128);
            entity.HasIndex(x => new { x.EntityType, x.EntityId, x.CreatedAtUtc });
        });
    }
}
