using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services;

public class PaymentOperationsService
{
    private readonly ApplicationDbContext _context;
    private readonly TransactionService _transactionService;
    private readonly IAuditLoggingService _auditLoggingService;

    public PaymentOperationsService(
        ApplicationDbContext context,
        TransactionService transactionService,
        IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _transactionService = transactionService;
        _auditLoggingService = auditLoggingService;
    }

    public async Task<List<BulkPaymentBatchDto>> GetBulkPaymentBatchesAsync()
    {
        var batches = await _context.BulkPaymentBatches
            .Include(b => b.Items)
            .OrderByDescending(b => b.CreatedAt)
            .Take(100)
            .ToListAsync();

        return batches.Select(MapBatchDto).ToList();
    }

    public async Task<BulkPaymentBatchDto?> GetBulkPaymentBatchAsync(string batchId)
    {
        var batch = await _context.BulkPaymentBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId);

        return batch == null ? null : MapBatchDto(batch);
    }

    public async Task<BulkPaymentBatchDto> CreateBulkPaymentBatchAsync(CreateBulkPaymentBatchRequest request)
    {
        if (request.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one bulk payment item is required.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var batch = new BulkPaymentBatch
        {
            Id = $"BPB-{timestamp}",
            BatchReference = $"BULK-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            Status = "PROCESSING",
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "GHS" : request.Currency.Trim().ToUpperInvariant(),
            Narration = request.Narration?.Trim(),
            SubmittedBy = string.IsNullOrWhiteSpace(request.SubmittedBy) ? null : request.SubmittedBy.Trim(),
            TotalAmount = request.Items.Sum(i => i.Amount),
            ItemCount = request.Items.Count,
            Items = request.Items.Select((item, index) => new BulkPaymentItem
            {
                Id = $"BPI-{timestamp}-{index + 1}",
                AccountId = item.AccountId.Trim(),
                TransactionType = item.TransactionType.Trim().ToUpperInvariant(),
                Amount = item.Amount,
                Narration = string.IsNullOrWhiteSpace(item.Narration) ? request.Narration?.Trim() : item.Narration.Trim(),
                TellerId = string.IsNullOrWhiteSpace(item.TellerId) ? null : item.TellerId.Trim(),
                ClientReference = string.IsNullOrWhiteSpace(item.ClientReference) ? null : item.ClientReference.Trim(),
                Status = "PENDING"
            }).ToList()
        };

        _context.BulkPaymentBatches.Add(batch);
        await _context.SaveChangesAsync();

        foreach (var item in batch.Items)
        {
            try
            {
                var posted = await _transactionService.PostTransactionAsync(new CreateTransactionRequest
                {
                    AccountId = item.AccountId,
                    Type = item.TransactionType,
                    Amount = item.Amount,
                    Narration = item.Narration,
                    TellerId = item.TellerId ?? batch.SubmittedBy,
                    ClientReference = item.ClientReference ?? $"{batch.BatchReference}-{item.Id}"
                });

                item.Status = "POSTED";
                item.PostedTransactionId = posted.Id;
                item.ProcessedAt = DateTime.UtcNow;
                batch.ProcessedAmount += item.Amount;
                batch.ProcessedCount++;
            }
            catch (Exception ex)
            {
                item.Status = "FAILED";
                item.ErrorMessage = ex.Message;
                item.ProcessedAt = DateTime.UtcNow;
                batch.FailedCount++;
            }
        }

        batch.Status = batch.FailedCount == 0
            ? "COMPLETED"
            : batch.ProcessedCount == 0
                ? "FAILED"
                : "PARTIAL";
        batch.ProcessedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "BULK_PAYMENT_BATCH",
            "BULK_PAYMENT_BATCH",
            batch.Id,
            batch.SubmittedBy,
            $"Processed bulk payment batch {batch.BatchReference}",
            status: batch.Status,
            newValues: new { batch.BatchReference, batch.ItemCount, batch.ProcessedCount, batch.FailedCount, batch.TotalAmount, batch.ProcessedAmount });

        return MapBatchDto(batch);
    }

    public async Task<List<ChequeClearingItemDto>> GetChequeItemsAsync()
    {
        var items = await _context.ChequeClearingItems
            .OrderByDescending(c => c.LodgedAt)
            .Take(200)
            .ToListAsync();

        return items.Select(MapChequeDto).ToList();
    }

    public async Task<ChequeClearingItemDto?> GetChequeItemAsync(string itemId)
    {
        var item = await _context.ChequeClearingItems.FirstOrDefaultAsync(c => c.Id == itemId);
        return item == null ? null : MapChequeDto(item);
    }

    public async Task<List<ChequeBookInventoryDto>> GetChequeBooksAsync(string? accountId = null)
    {
        var query = _context.ChequeBookInventories
            .Include(b => b.Leaves)
            .OrderByDescending(b => b.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(accountId))
        {
            query = query.Where(b => b.AccountId == accountId.Trim());
        }

        var books = await query.Take(200).ToListAsync();
        return books.Select(MapChequeBookDto).ToList();
    }

    public async Task<ChequeBookInventoryDto?> GetChequeBookAsync(string bookId)
    {
        var book = await _context.ChequeBookInventories
            .Include(b => b.Leaves.OrderBy(l => l.SerialNumber))
            .FirstOrDefaultAsync(b => b.Id == bookId);

        return book == null ? null : MapChequeBookDto(book);
    }

    public async Task<ChequeBookInventoryDto> CreateChequeBookStockAsync(CreateChequeBookStockRequest request)
    {
        var branchId = request.BranchId.Trim();
        var prefix = request.SeriesPrefix.Trim().ToUpperInvariant();
        var startSerial = request.StartSerialNumber;
        var endSerial = startSerial + request.LeafCount - 1;

        var existingOverlap = await _context.ChequeBookLeaves.AnyAsync(l =>
            l.ChequeNumber.StartsWith(prefix) &&
            l.SerialNumber >= startSerial &&
            l.SerialNumber <= endSerial);

        if (existingOverlap)
        {
            throw new InvalidOperationException("The requested cheque serial range overlaps with an existing cheque book.");
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var book = new ChequeBookInventory
        {
            Id = $"CHQB-{timestamp}",
            BookReference = $"CHQBK-{DateTime.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}",
            BranchId = branchId,
            SeriesPrefix = prefix,
            StartSerialNumber = startSerial,
            EndSerialNumber = endSerial,
            LeafCount = request.LeafCount,
            AvailableLeafCount = request.LeafCount,
            UsedLeafCount = 0,
            CancelledLeafCount = 0,
            Status = "IN_STOCK",
            Remarks = request.Remarks?.Trim(),
            Leaves = Enumerable.Range(0, request.LeafCount).Select(index =>
            {
                var serial = startSerial + index;
                return new ChequeBookLeaf
                {
                    Id = $"CHQL-{timestamp}-{index + 1}",
                    SerialNumber = serial,
                    ChequeNumber = $"{prefix}{serial:D6}",
                    Status = "AVAILABLE"
                };
            }).ToList()
        };

        _context.ChequeBookInventories.Add(book);
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_BOOK_STOCKED",
            "CHEQUE_BOOK",
            book.Id,
            null,
            $"Cheque book stock received for range {book.SeriesPrefix}{book.StartSerialNumber:D6} to {book.SeriesPrefix}{book.EndSerialNumber:D6}",
            status: "SUCCESS",
            newValues: new { book.BookReference, book.BranchId, book.SeriesPrefix, book.StartSerialNumber, book.EndSerialNumber, book.LeafCount });

        return MapChequeBookDto(book);
    }

    public async Task<ChequeBookInventoryDto> IssueChequeBookAsync(string bookId, IssueChequeBookRequest request)
    {
        var book = await _context.ChequeBookInventories
            .Include(b => b.Leaves)
            .FirstOrDefaultAsync(b => b.Id == bookId)
            ?? throw new InvalidOperationException("Cheque book inventory record not found.");

        if (!string.Equals(book.Status, "IN_STOCK", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only cheque books in stock can be issued.");
        }

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        book.AccountId = account.Id;
        book.CustomerId = account.CustomerId;
        book.IssuedAt = DateTime.UtcNow;
        book.IssuedBy = string.IsNullOrWhiteSpace(request.IssuedBy) ? null : request.IssuedBy.Trim();
        book.Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? book.Remarks : request.Remarks.Trim();
        book.Status = "ISSUED";

        foreach (var leaf in book.Leaves)
        {
            leaf.AccountId = account.Id;
            leaf.Status = "ISSUED";
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_BOOK_ISSUED",
            "CHEQUE_BOOK",
            book.Id,
            book.IssuedBy,
            $"Cheque book {book.BookReference} issued to account {account.Id}",
            status: "SUCCESS",
            newValues: new { book.BookReference, account.Id, book.CustomerId, book.LeafCount });

        return MapChequeBookDto(book);
    }

    public async Task<ChequeBookInventoryDto> CancelChequeLeafAsync(string leafId, string reason, string? userId)
    {
        var leaf = await _context.ChequeBookLeaves
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == leafId)
            ?? throw new InvalidOperationException("Cheque leaf not found.");

        if (leaf.Status is "USED" or "CANCELLED")
        {
            throw new InvalidOperationException("Used or already cancelled cheque leaves cannot be cancelled again.");
        }

        leaf.Status = "CANCELLED";
        leaf.CancelReason = reason.Trim();

        if (leaf.Book != null)
        {
            leaf.Book.CancelledLeafCount += 1;
            if (leaf.Book.AvailableLeafCount > 0)
            {
                leaf.Book.AvailableLeafCount -= 1;
            }

            leaf.Book.Status = leaf.Book.AvailableLeafCount == 0
                ? "EXHAUSTED"
                : leaf.Book.UsedLeafCount > 0
                    ? "ACTIVE"
                    : leaf.Book.Status;
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_LEAF_CANCELLED",
            "CHEQUE_BOOK_LEAF",
            leaf.Id,
            userId,
            $"Cheque leaf {leaf.ChequeNumber} cancelled",
            status: "SUCCESS",
            newValues: new { leaf.ChequeNumber, leaf.CancelReason, BookId = leaf.BookId });

        return leaf.Book == null
            ? throw new InvalidOperationException("Cheque book inventory record not found for the cancelled leaf.")
            : MapChequeBookDto(leaf.Book);
    }

    public async Task<ChequeBookInventoryDto> MarkChequeLeafUsedHistoricallyAsync(MarkChequeLeafUsedRequest request, string? userId)
    {
        var chequeNumber = request.ChequeNumber.Trim();
        var accountId = request.AccountId.Trim();

        var leaf = await _context.ChequeBookLeaves
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l =>
                l.ChequeNumber == chequeNumber &&
                l.AccountId == accountId &&
                l.Status == "ISSUED")
            ?? throw new InvalidOperationException("Cheque leaf not found in issued inventory for the supplied account.");

        if (leaf.Book == null)
        {
            throw new InvalidOperationException("Cheque book inventory record not found for the supplied leaf.");
        }

        var usedAt = request.UsedAt ?? DateTime.UtcNow;
        var historicalTransactionId = string.IsNullOrWhiteSpace(request.HistoricalTransactionId)
            ? $"LEGACY-{chequeNumber}"
            : request.HistoricalTransactionId.Trim();

        leaf.Status = "USED";
        leaf.UsedAt = usedAt;
        leaf.UsedTransactionId = historicalTransactionId;

        leaf.Book.UsedLeafCount += 1;
        leaf.Book.AvailableLeafCount = Math.Max(0, leaf.Book.AvailableLeafCount - 1);
        leaf.Book.Status = leaf.Book.AvailableLeafCount == 0 ? "EXHAUSTED" : "ACTIVE";

        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            leaf.Book.Remarks = string.IsNullOrWhiteSpace(leaf.Book.Remarks)
                ? request.Remarks.Trim()
                : $"{leaf.Book.Remarks}; {request.Remarks.Trim()}";
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_LEAF_MARKED_USED_HISTORY",
            "CHEQUE_BOOK_LEAF",
            leaf.Id,
            userId,
            $"Cheque leaf {leaf.ChequeNumber} marked as historically used.",
            status: "SUCCESS",
            newValues: new
            {
                leaf.ChequeNumber,
                AccountId = accountId,
                leaf.UsedTransactionId,
                leaf.UsedAt,
                request.Remarks
            });

        return MapChequeBookDto(leaf.Book);
    }

    public async Task<ChequeClearingItemDto> LodgeChequeDepositAsync(LodgeChequeDepositRequest request)
    {
        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        if (!string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Account is not active.");
        }

        var businessDate = await ResolveBusinessDateAsync();
        var holdDays = request.IsOtherBankCheque
            ? await GetIntConfigAsync("payments.cheque.other_bank_hold_days", 2)
            : await GetIntConfigAsync("payments.cheque.same_bank_hold_days", 0);

        var item = new ChequeClearingItem
        {
            Id = $"CHQ-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            AccountId = account.Id,
            TransactionType = "DEPOSIT",
            ChequeNumber = request.ChequeNumber.Trim(),
            DrawerName = request.DrawerName?.Trim(),
            DrawerAccountNumber = request.DrawerAccountNumber?.Trim(),
            PresentingBankCode = request.PresentingBankCode.Trim().ToUpperInvariant(),
            DraweeBankCode = request.DraweeBankCode.Trim().ToUpperInvariant(),
            ClearingChannel = string.IsNullOrWhiteSpace(request.ClearingChannel) ? "GHIPSS" : request.ClearingChannel.Trim().ToUpperInvariant(),
            BogRegulatoryClass = string.IsNullOrWhiteSpace(request.BogRegulatoryClass) ? "LOCAL" : request.BogRegulatoryClass.Trim().ToUpperInvariant(),
            IsOtherBankCheque = request.IsOtherBankCheque,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency.Trim().ToUpperInvariant(),
            Status = holdDays == 0 ? "PENDING_CLEARING" : "LODGED",
            HoldDays = holdDays,
            LodgedBy = string.IsNullOrWhiteSpace(request.TellerId) ? null : request.TellerId.Trim(),
            LodgedAt = DateTime.UtcNow,
            ClearingDate = businessDate.AddDays(holdDays),
            Narration = request.Narration?.Trim()
        };

        _context.ChequeClearingItems.Add(item);
        await _context.SaveChangesAsync();

        if (item.ClearingDate <= businessDate)
        {
            await ClearChequeAsync(item, item.LodgedBy, item.LodgedBy);
        }

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_DEPOSIT_LODGED",
            "CHEQUE",
            item.Id,
            item.LodgedBy,
            $"Cheque {item.ChequeNumber} lodged for clearing",
            status: item.Status,
            newValues: new { item.AccountId, item.Amount, item.ClearingDate, item.ClearingChannel, item.DraweeBankCode, item.PresentingBankCode, item.IsOtherBankCheque });

        return MapChequeDto(item);
    }

    public async Task<ChequeClearingItemDto> ProcessChequeWithdrawalAsync(ChequeWithdrawalRequest request)
    {
        var chequeLeaf = await _context.ChequeBookLeaves
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l =>
                l.ChequeNumber == request.ChequeNumber.Trim() &&
                l.AccountId == request.AccountId &&
                l.Status == "ISSUED");

        if (chequeLeaf == null)
        {
            throw new InvalidOperationException("Cheque number is not available in issued cheque-book inventory for this account.");
        }

        var posted = await _transactionService.PostTransactionAsync(new CreateTransactionRequest
        {
            AccountId = request.AccountId,
            Type = "WITHDRAWAL",
            Amount = request.Amount,
            Narration = string.IsNullOrWhiteSpace(request.Narration)
                ? $"Cheque withdrawal {request.ChequeNumber}"
                : request.Narration.Trim(),
            TellerId = request.TellerId,
            ClientReference = $"CHQ-WDL-{request.ChequeNumber}"
        });

        var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == request.AccountId)
            ?? throw new InvalidOperationException("Account not found.");

        var item = new ChequeClearingItem
        {
            Id = $"CHQ-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
            AccountId = request.AccountId,
            TransactionType = "WITHDRAWAL",
            ChequeNumber = request.ChequeNumber.Trim(),
            PresentingBankCode = account.BranchId ?? "BANKINSIGHT",
            DraweeBankCode = account.BranchId ?? "BANKINSIGHT",
            ClearingChannel = "INTERNAL",
            BogRegulatoryClass = "ONUS",
            IsOtherBankCheque = false,
            Amount = request.Amount,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? account.Currency : request.Currency.Trim().ToUpperInvariant(),
            Status = "PAID",
            HoldDays = 0,
            LodgedBy = request.TellerId.Trim(),
            LodgedAt = DateTime.UtcNow,
            ClearingDate = DateOnly.FromDateTime(DateTime.UtcNow),
            ClearedAt = DateTime.UtcNow,
            PostedTransactionId = posted.Id,
            Narration = request.Narration?.Trim()
        };

        _context.ChequeClearingItems.Add(item);

        chequeLeaf.Status = "USED";
        chequeLeaf.UsedAt = DateTime.UtcNow;
        chequeLeaf.UsedTransactionId = posted.Id;

        if (chequeLeaf.Book != null)
        {
            chequeLeaf.Book.UsedLeafCount += 1;
            chequeLeaf.Book.AvailableLeafCount = Math.Max(0, chequeLeaf.Book.AvailableLeafCount - 1);
            chequeLeaf.Book.Status = chequeLeaf.Book.AvailableLeafCount == 0 ? "EXHAUSTED" : "ACTIVE";
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_WITHDRAWAL_PAID",
            "CHEQUE",
            item.Id,
            item.LodgedBy,
            $"Cheque withdrawal {item.ChequeNumber} paid",
            status: "SUCCESS",
            newValues: new { item.AccountId, item.Amount, item.PostedTransactionId });

        return MapChequeDto(item);
    }

    public async Task<ChequeClearingItemDto> ReturnChequeAsync(string itemId, string reason, string? userId)
    {
        var item = await _context.ChequeClearingItems.FirstOrDefaultAsync(c => c.Id == itemId)
            ?? throw new InvalidOperationException("Cheque item not found.");

        if (string.Equals(item.Status, "CLEARED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.Status, "PAID", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Cleared or paid cheques cannot be returned through this endpoint.");
        }

        item.Status = "RETURNED";
        item.ReturnReason = reason.Trim();
        item.FailureReason = null;
        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_RETURNED",
            "CHEQUE",
            item.Id,
            userId,
            $"Cheque {item.ChequeNumber} returned",
            status: "SUCCESS",
            newValues: new { item.ChequeNumber, item.ReturnReason });

        return MapChequeDto(item);
    }

    public async Task<ChequeClearingBatchResultDto> ProcessDueChequeClearingsAsync(DateOnly businessDate, string? userId)
    {
        var items = await _context.ChequeClearingItems
            .Where(c => c.TransactionType == "DEPOSIT" &&
                        (c.Status == "LODGED" || c.Status == "PENDING_CLEARING") &&
                        c.ClearingDate <= businessDate)
            .ToListAsync();

        var result = new ChequeClearingBatchResultDto
        {
            BusinessDate = businessDate,
            ItemsEvaluated = items.Count
        };

        foreach (var item in items)
        {
            try
            {
                await ClearChequeAsync(item, userId, null);
                result.ItemsCleared++;
                result.TotalClearedAmount += item.Amount;
                result.ClearedItemIds.Add(item.Id);
            }
            catch (Exception ex)
            {
                item.Status = "PENDING_CLEARING";
                item.FailureReason = ex.Message;
                result.ItemsPending++;
                result.PendingItemIds.Add(item.Id);
            }
        }

        await _context.SaveChangesAsync();

        await _auditLoggingService.LogActionAsync(
            "CHEQUE_CLEARING_BATCH",
            "SYSTEM",
            businessDate.ToString("yyyy-MM-dd"),
            userId,
            $"Cheque clearing batch completed for {businessDate:yyyy-MM-dd}",
            status: "SUCCESS",
            newValues: new { result.ItemsEvaluated, result.ItemsCleared, result.ItemsPending, result.TotalClearedAmount, result.ClearedItemIds, result.PendingItemIds });

        return result;
    }

    private async Task ClearChequeAsync(ChequeClearingItem item, string? createdBy, string? tellerId)
    {
        var posted = await _transactionService.PostTransactionAsync(
            new CreateTransactionRequest
            {
                AccountId = item.AccountId,
                Type = "DEPOSIT",
                Amount = item.Amount,
                Narration = string.IsNullOrWhiteSpace(item.Narration)
                    ? $"Cheque clearing {item.ChequeNumber}"
                    : item.Narration,
                TellerId = tellerId,
                ClientReference = $"CHQ-CLR-{item.Id}"
            },
            allowSystemTeller: string.IsNullOrWhiteSpace(tellerId),
            createdBy: createdBy);

        item.Status = "CLEARED";
        item.PostedTransactionId = posted.Id;
        item.ClearedAt = DateTime.UtcNow;
        item.FailureReason = null;
    }

    private static BulkPaymentBatchDto MapBatchDto(BulkPaymentBatch batch)
    {
        return new BulkPaymentBatchDto
        {
            Id = batch.Id,
            BatchReference = batch.BatchReference,
            Status = batch.Status,
            Currency = batch.Currency,
            Narration = batch.Narration,
            TotalAmount = batch.TotalAmount,
            ProcessedAmount = batch.ProcessedAmount,
            ItemCount = batch.ItemCount,
            ProcessedCount = batch.ProcessedCount,
            FailedCount = batch.FailedCount,
            CreatedAt = batch.CreatedAt,
            ProcessedAt = batch.ProcessedAt,
            Items = batch.Items
                .OrderBy(i => i.CreatedAt)
                .Select(i => new BulkPaymentItemDto
                {
                    Id = i.Id,
                    AccountId = i.AccountId,
                    TransactionType = i.TransactionType,
                    Amount = i.Amount,
                    Narration = i.Narration,
                    TellerId = i.TellerId,
                    ClientReference = i.ClientReference,
                    Status = i.Status,
                    PostedTransactionId = i.PostedTransactionId,
                    ErrorMessage = i.ErrorMessage,
                    ProcessedAt = i.ProcessedAt
                })
                .ToList()
        };
    }

    private static ChequeClearingItemDto MapChequeDto(ChequeClearingItem item)
    {
        return new ChequeClearingItemDto
        {
            Id = item.Id,
            AccountId = item.AccountId,
            TransactionType = item.TransactionType,
            ChequeNumber = item.ChequeNumber,
            DrawerName = item.DrawerName,
            DrawerAccountNumber = item.DrawerAccountNumber,
            PresentingBankCode = item.PresentingBankCode,
            DraweeBankCode = item.DraweeBankCode,
            ClearingChannel = item.ClearingChannel,
            BogRegulatoryClass = item.BogRegulatoryClass,
            IsOtherBankCheque = item.IsOtherBankCheque,
            Amount = item.Amount,
            Currency = item.Currency,
            Status = item.Status,
            HoldDays = item.HoldDays,
            LodgedAt = item.LodgedAt,
            ClearingDate = item.ClearingDate,
            ClearedAt = item.ClearedAt,
            PostedTransactionId = item.PostedTransactionId,
            ReturnReason = item.ReturnReason,
            FailureReason = item.FailureReason,
            Narration = item.Narration
        };
    }

    private static ChequeBookInventoryDto MapChequeBookDto(ChequeBookInventory book)
    {
        return new ChequeBookInventoryDto
        {
            Id = book.Id,
            BookReference = book.BookReference,
            BranchId = book.BranchId,
            SeriesPrefix = book.SeriesPrefix,
            StartSerialNumber = book.StartSerialNumber,
            EndSerialNumber = book.EndSerialNumber,
            LeafCount = book.LeafCount,
            AvailableLeafCount = book.AvailableLeafCount,
            UsedLeafCount = book.UsedLeafCount,
            CancelledLeafCount = book.CancelledLeafCount,
            Status = book.Status,
            AccountId = book.AccountId,
            CustomerId = book.CustomerId,
            IssuedAt = book.IssuedAt,
            IssuedBy = book.IssuedBy,
            Remarks = book.Remarks,
            CreatedAt = book.CreatedAt,
            Leaves = book.Leaves
                .OrderBy(l => l.SerialNumber)
                .Select(l => new ChequeBookLeafDto
                {
                    Id = l.Id,
                    SerialNumber = l.SerialNumber,
                    ChequeNumber = l.ChequeNumber,
                    Status = l.Status,
                    AccountId = l.AccountId,
                    UsedTransactionId = l.UsedTransactionId,
                    UsedAt = l.UsedAt,
                    CancelReason = l.CancelReason
                })
                .ToList()
        };
    }

    private async Task<DateOnly> ResolveBusinessDateAsync()
    {
        var configured = await _context.SystemConfigs
            .Where(c => c.Key == "SYSTEM_BUSINESS_DATE")
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        return DateOnly.TryParse(configured, out var businessDate)
            ? businessDate
            : DateOnly.FromDateTime(DateTime.UtcNow);
    }

    private async Task<int> GetIntConfigAsync(string key, int defaultValue)
    {
        var value = await _context.SystemConfigs
            .Where(c => c.Key == key)
            .Select(c => c.Value)
            .FirstOrDefaultAsync();

        return int.TryParse(value, out var parsed) ? parsed : defaultValue;
    }
}
