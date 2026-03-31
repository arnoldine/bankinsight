using System.Globalization;

namespace CoreBanker.Services
{
    public class TransactionService : ApiClientBase
    {
        public TransactionService(HttpClient httpClient, CoreBanker.State.AppState appState) : base(httpClient, appState) { }

        public async Task<List<TransactionDto>> GetTransactionsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<TransactionApiModel>>("/api/transactions", cancellationToken);
            return (result ?? new List<TransactionApiModel>()).ConvertAll(MapTransaction);
        }

        public async Task<List<BulkPaymentBatchDto>> GetBulkPaymentBatchesAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<BulkPaymentBatchApiModel>>("/api/payments/bulk", cancellationToken);
            return (result ?? new List<BulkPaymentBatchApiModel>()).ConvertAll(MapBulkBatch);
        }

        public async Task<BulkPaymentBatchDto?> CreateBulkPaymentBatchAsync(BulkPaymentBatchRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<BulkPaymentBatchRequest, BulkPaymentBatchApiModel>("/api/payments/bulk", request, cancellationToken);
            return response is null ? null : MapBulkBatch(response);
        }

        public async Task<List<ChequeClearingItemDto>> GetChequeItemsAsync(CancellationToken cancellationToken = default)
        {
            var result = await GetAsync<List<ChequeClearingItemApiModel>>("/api/payments/cheques", cancellationToken);
            return (result ?? new List<ChequeClearingItemApiModel>()).ConvertAll(MapChequeItem);
        }

        public async Task<ChequeClearingItemDto?> LodgeChequeDepositAsync(ChequeDepositRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<ChequeDepositRequest, ChequeClearingItemApiModel>("/api/payments/cheques/deposits", request, cancellationToken);
            return response is null ? null : MapChequeItem(response);
        }

        public async Task<ChequeClearingItemDto?> ProcessChequeWithdrawalAsync(ChequeWithdrawalRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<ChequeWithdrawalRequest, ChequeClearingItemApiModel>("/api/payments/cheques/withdrawals", request, cancellationToken);
            return response is null ? null : MapChequeItem(response);
        }

        public async Task<ChequeClearingItemDto?> ReturnChequeAsync(string itemId, string reason, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<ReturnChequeRequest, ChequeClearingItemApiModel>($"/api/payments/cheques/{itemId}/return", new ReturnChequeRequest { Reason = reason }, cancellationToken);
            return response is null ? null : MapChequeItem(response);
        }

        public async Task<List<ChequeBookInventoryDto>> GetChequeBooksAsync(string? accountId = null, CancellationToken cancellationToken = default)
        {
            var path = string.IsNullOrWhiteSpace(accountId)
                ? "/api/payments/cheque-books"
                : $"/api/payments/cheque-books?accountId={Uri.EscapeDataString(accountId)}";
            var result = await GetAsync<List<ChequeBookInventoryApiModel>>(path, cancellationToken);
            return (result ?? new List<ChequeBookInventoryApiModel>()).ConvertAll(MapChequeBook);
        }

        public async Task<ChequeBookInventoryDto?> CreateChequeBookStockAsync(CreateChequeBookStockRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<CreateChequeBookStockRequest, ChequeBookInventoryApiModel>("/api/payments/cheque-books/stock", request, cancellationToken);
            return response is null ? null : MapChequeBook(response);
        }

        public async Task<ChequeBookInventoryDto?> IssueChequeBookAsync(string bookId, IssueChequeBookRequest request, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<IssueChequeBookRequest, ChequeBookInventoryApiModel>($"/api/payments/cheque-books/{bookId}/issue", request, cancellationToken);
            return response is null ? null : MapChequeBook(response);
        }

        public async Task<ChequeBookInventoryDto?> CancelChequeLeafAsync(string leafId, string reason, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync<CancelChequeLeafRequest, ChequeBookInventoryApiModel>($"/api/payments/cheque-books/leaves/{leafId}/cancel", new CancelChequeLeafRequest { Reason = reason }, cancellationToken);
            return response is null ? null : MapChequeBook(response);
        }

        private static TransactionDto MapTransaction(TransactionApiModel txn)
        {
            return new TransactionDto
            {
                Id = txn.Id ?? string.Empty,
                AccountId = txn.AccountId ?? string.Empty,
                Type = NormalizeType(txn.Type),
                Amount = txn.Amount ?? 0m,
                Date = ParseDate(txn.Date),
                Status = NormalizeStatus(txn.Status),
                Narration = txn.Narration ?? string.Empty,
                Reference = txn.Reference ?? string.Empty,
                TellerId = txn.TellerId ?? string.Empty
            };
        }

        private static BulkPaymentBatchDto MapBulkBatch(BulkPaymentBatchApiModel batch)
        {
            return new BulkPaymentBatchDto
            {
                Id = batch.Id ?? string.Empty,
                BatchReference = batch.BatchReference ?? string.Empty,
                Status = NormalizeStatus(batch.Status),
                Currency = batch.Currency ?? "GHS",
                Narration = batch.Narration ?? string.Empty,
                TotalAmount = batch.TotalAmount ?? 0m,
                ProcessedAmount = batch.ProcessedAmount ?? 0m,
                ItemCount = batch.ItemCount ?? 0,
                ProcessedCount = batch.ProcessedCount ?? 0,
                FailedCount = batch.FailedCount ?? 0,
                CreatedAt = ParseDate(batch.CreatedAt),
                ProcessedAt = string.IsNullOrWhiteSpace(batch.ProcessedAt) ? null : ParseDate(batch.ProcessedAt),
                Items = (batch.Items ?? new List<BulkPaymentItemApiModel>()).ConvertAll(MapBulkItem)
            };
        }

        private static BulkPaymentItemDto MapBulkItem(BulkPaymentItemApiModel item)
        {
            return new BulkPaymentItemDto
            {
                Id = item.Id ?? string.Empty,
                AccountId = item.AccountId ?? string.Empty,
                TransactionType = NormalizeType(item.TransactionType),
                Amount = item.Amount ?? 0m,
                Narration = item.Narration ?? string.Empty,
                TellerId = item.TellerId ?? string.Empty,
                ClientReference = item.ClientReference ?? string.Empty,
                Status = NormalizeStatus(item.Status),
                PostedTransactionId = item.PostedTransactionId ?? string.Empty,
                ErrorMessage = item.ErrorMessage ?? string.Empty,
                ProcessedAt = string.IsNullOrWhiteSpace(item.ProcessedAt) ? null : ParseDate(item.ProcessedAt)
            };
        }

        private static ChequeClearingItemDto MapChequeItem(ChequeClearingItemApiModel item)
        {
            return new ChequeClearingItemDto
            {
                Id = item.Id ?? string.Empty,
                AccountId = item.AccountId ?? string.Empty,
                TransactionType = NormalizeType(item.TransactionType),
                ChequeNumber = item.ChequeNumber ?? string.Empty,
                DrawerName = item.DrawerName ?? string.Empty,
                DrawerAccountNumber = item.DrawerAccountNumber ?? string.Empty,
                PresentingBankCode = item.PresentingBankCode ?? string.Empty,
                DraweeBankCode = item.DraweeBankCode ?? string.Empty,
                ClearingChannel = item.ClearingChannel ?? string.Empty,
                BogRegulatoryClass = item.BogRegulatoryClass ?? string.Empty,
                IsOtherBankCheque = item.IsOtherBankCheque ?? false,
                Amount = item.Amount ?? 0m,
                Currency = item.Currency ?? "GHS",
                Status = NormalizeStatus(item.Status),
                HoldDays = item.HoldDays ?? 0,
                LodgedAt = ParseDate(item.LodgedAt),
                ClearingDate = item.ClearingDate ?? string.Empty,
                ClearedAt = string.IsNullOrWhiteSpace(item.ClearedAt) ? null : ParseDate(item.ClearedAt),
                PostedTransactionId = item.PostedTransactionId ?? string.Empty,
                ReturnReason = item.ReturnReason ?? string.Empty,
                FailureReason = item.FailureReason ?? string.Empty,
                Narration = item.Narration ?? string.Empty
            };
        }

        private static ChequeBookInventoryDto MapChequeBook(ChequeBookInventoryApiModel book)
        {
            return new ChequeBookInventoryDto
            {
                Id = book.Id ?? string.Empty,
                BookReference = book.BookReference ?? string.Empty,
                BranchId = book.BranchId ?? string.Empty,
                SeriesPrefix = book.SeriesPrefix ?? string.Empty,
                StartSerialNumber = book.StartSerialNumber ?? 0,
                EndSerialNumber = book.EndSerialNumber ?? 0,
                LeafCount = book.LeafCount ?? 0,
                AvailableLeafCount = book.AvailableLeafCount ?? 0,
                UsedLeafCount = book.UsedLeafCount ?? 0,
                CancelledLeafCount = book.CancelledLeafCount ?? 0,
                Status = book.Status ?? string.Empty,
                AccountId = book.AccountId ?? string.Empty,
                CustomerId = book.CustomerId ?? string.Empty,
                IssuedAt = string.IsNullOrWhiteSpace(book.IssuedAt) ? null : ParseDate(book.IssuedAt),
                IssuedBy = book.IssuedBy ?? string.Empty,
                Remarks = book.Remarks ?? string.Empty,
                CreatedAt = ParseDate(book.CreatedAt),
                Leaves = (book.Leaves ?? new List<ChequeBookLeafApiModel>()).ConvertAll(MapChequeBookLeaf)
            };
        }

        private static ChequeBookLeafDto MapChequeBookLeaf(ChequeBookLeafApiModel leaf)
        {
            return new ChequeBookLeafDto
            {
                Id = leaf.Id ?? string.Empty,
                SerialNumber = leaf.SerialNumber ?? 0,
                ChequeNumber = leaf.ChequeNumber ?? string.Empty,
                Status = leaf.Status ?? string.Empty,
                AccountId = leaf.AccountId ?? string.Empty,
                UsedTransactionId = leaf.UsedTransactionId ?? string.Empty,
                UsedAt = string.IsNullOrWhiteSpace(leaf.UsedAt) ? null : ParseDate(leaf.UsedAt),
                CancelReason = leaf.CancelReason ?? string.Empty
            };
        }

        private static DateTime ParseDate(string? value)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed
                : DateTime.UtcNow;
        }

        private static string NormalizeType(string? value)
        {
            var normalized = (value ?? "DEPOSIT").Trim().ToUpperInvariant();
            return normalized switch
            {
                "WITHDRAWAL" => "WITHDRAWAL",
                "TRANSFER" => "TRANSFER",
                "LOAN_REPAYMENT" => "LOAN_REPAYMENT",
                "DEPOSIT" => "DEPOSIT",
                _ => "DEPOSIT"
            };
        }

        private static string NormalizeStatus(string? value)
        {
            var normalized = (value ?? "POSTED").Trim().ToUpperInvariant();
            return normalized switch
            {
                "PENDING" => "PENDING",
                "FAILED" => "FAILED",
                "PROCESSING" => "PROCESSING",
                "COMPLETED" => "COMPLETED",
                "PARTIAL" => "PARTIAL",
                "LODGED" => "LODGED",
                "CLEARED" => "CLEARED",
                "PAID" => "PAID",
                "RETURNED" => "RETURNED",
                "PENDING_CLEARING" => "PENDING_CLEARING",
                _ => "POSTED"
            };
        }

        private sealed class TransactionApiModel
        {
            public string? Id { get; set; }
            public string? AccountId { get; set; }
            public string? Type { get; set; }
            public decimal? Amount { get; set; }
            public string? Date { get; set; }
            public string? Status { get; set; }
            public string? Narration { get; set; }
            public string? Reference { get; set; }
            public string? TellerId { get; set; }
        }

        private sealed class BulkPaymentBatchApiModel
        {
            public string? Id { get; set; }
            public string? BatchReference { get; set; }
            public string? Status { get; set; }
            public string? Currency { get; set; }
            public string? Narration { get; set; }
            public decimal? TotalAmount { get; set; }
            public decimal? ProcessedAmount { get; set; }
            public int? ItemCount { get; set; }
            public int? ProcessedCount { get; set; }
            public int? FailedCount { get; set; }
            public string? CreatedAt { get; set; }
            public string? ProcessedAt { get; set; }
            public List<BulkPaymentItemApiModel>? Items { get; set; }
        }

        private sealed class BulkPaymentItemApiModel
        {
            public string? Id { get; set; }
            public string? AccountId { get; set; }
            public string? TransactionType { get; set; }
            public decimal? Amount { get; set; }
            public string? Narration { get; set; }
            public string? TellerId { get; set; }
            public string? ClientReference { get; set; }
            public string? Status { get; set; }
            public string? PostedTransactionId { get; set; }
            public string? ErrorMessage { get; set; }
            public string? ProcessedAt { get; set; }
        }

        private sealed class ChequeClearingItemApiModel
        {
            public string? Id { get; set; }
            public string? AccountId { get; set; }
            public string? TransactionType { get; set; }
            public string? ChequeNumber { get; set; }
            public string? DrawerName { get; set; }
            public string? DrawerAccountNumber { get; set; }
            public string? PresentingBankCode { get; set; }
            public string? DraweeBankCode { get; set; }
            public string? ClearingChannel { get; set; }
            public string? BogRegulatoryClass { get; set; }
            public bool? IsOtherBankCheque { get; set; }
            public decimal? Amount { get; set; }
            public string? Currency { get; set; }
            public string? Status { get; set; }
            public int? HoldDays { get; set; }
            public string? LodgedAt { get; set; }
            public string? ClearingDate { get; set; }
            public string? ClearedAt { get; set; }
            public string? PostedTransactionId { get; set; }
            public string? ReturnReason { get; set; }
            public string? FailureReason { get; set; }
            public string? Narration { get; set; }
        }

        private sealed class ChequeBookInventoryApiModel
        {
            public string? Id { get; set; }
            public string? BookReference { get; set; }
            public string? BranchId { get; set; }
            public string? SeriesPrefix { get; set; }
            public long? StartSerialNumber { get; set; }
            public long? EndSerialNumber { get; set; }
            public int? LeafCount { get; set; }
            public int? AvailableLeafCount { get; set; }
            public int? UsedLeafCount { get; set; }
            public int? CancelledLeafCount { get; set; }
            public string? Status { get; set; }
            public string? AccountId { get; set; }
            public string? CustomerId { get; set; }
            public string? IssuedAt { get; set; }
            public string? IssuedBy { get; set; }
            public string? Remarks { get; set; }
            public string? CreatedAt { get; set; }
            public List<ChequeBookLeafApiModel>? Leaves { get; set; }
        }

        private sealed class ChequeBookLeafApiModel
        {
            public string? Id { get; set; }
            public long? SerialNumber { get; set; }
            public string? ChequeNumber { get; set; }
            public string? Status { get; set; }
            public string? AccountId { get; set; }
            public string? UsedTransactionId { get; set; }
            public string? UsedAt { get; set; }
            public string? CancelReason { get; set; }
        }
    }

    public class TransactionDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = "POSTED";
        public string Narration { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TellerId { get; set; } = string.Empty;
    }

    public class BulkPaymentBatchRequest
    {
        public string Currency { get; set; } = "GHS";
        public string? Narration { get; set; }
        public string? SubmittedBy { get; set; }
        public List<BulkPaymentItemRequest> Items { get; set; } = new();
    }

    public class BulkPaymentItemRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = "DEPOSIT";
        public decimal Amount { get; set; }
        public string? Narration { get; set; }
        public string? TellerId { get; set; }
        public string? ClientReference { get; set; }
    }

    public class BulkPaymentBatchDto
    {
        public string Id { get; set; } = string.Empty;
        public string BatchReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = "GHS";
        public string Narration { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal ProcessedAmount { get; set; }
        public int ItemCount { get; set; }
        public int ProcessedCount { get; set; }
        public int FailedCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public List<BulkPaymentItemDto> Items { get; set; } = new();
    }

    public class BulkPaymentItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Narration { get; set; } = string.Empty;
        public string TellerId { get; set; } = string.Empty;
        public string ClientReference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PostedTransactionId { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime? ProcessedAt { get; set; }
    }

    public class ChequeDepositRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string ChequeNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GHS";
        public string? DrawerName { get; set; }
        public string? DrawerAccountNumber { get; set; }
        public string PresentingBankCode { get; set; } = string.Empty;
        public string DraweeBankCode { get; set; } = string.Empty;
        public bool IsOtherBankCheque { get; set; } = true;
        public string ClearingChannel { get; set; } = "GHIPSS";
        public string BogRegulatoryClass { get; set; } = "LOCAL";
        public string? TellerId { get; set; }
        public string? Narration { get; set; }
    }

    public class ChequeWithdrawalRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string ChequeNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GHS";
        public string TellerId { get; set; } = string.Empty;
        public string? Narration { get; set; }
    }

    public class ReturnChequeRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ChequeClearingItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public string ChequeNumber { get; set; } = string.Empty;
        public string DrawerName { get; set; } = string.Empty;
        public string DrawerAccountNumber { get; set; } = string.Empty;
        public string PresentingBankCode { get; set; } = string.Empty;
        public string DraweeBankCode { get; set; } = string.Empty;
        public string ClearingChannel { get; set; } = string.Empty;
        public string BogRegulatoryClass { get; set; } = string.Empty;
        public bool IsOtherBankCheque { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GHS";
        public string Status { get; set; } = string.Empty;
        public int HoldDays { get; set; }
        public DateTime LodgedAt { get; set; }
        public string ClearingDate { get; set; } = string.Empty;
        public DateTime? ClearedAt { get; set; }
        public string PostedTransactionId { get; set; } = string.Empty;
        public string ReturnReason { get; set; } = string.Empty;
        public string FailureReason { get; set; } = string.Empty;
        public string Narration { get; set; } = string.Empty;
    }

    public class CreateChequeBookStockRequest
    {
        public string BranchId { get; set; } = string.Empty;
        public string SeriesPrefix { get; set; } = string.Empty;
        public long StartSerialNumber { get; set; }
        public int LeafCount { get; set; }
        public string? Remarks { get; set; }
    }

    public class IssueChequeBookRequest
    {
        public string AccountId { get; set; } = string.Empty;
        public string? IssuedBy { get; set; }
        public string? Remarks { get; set; }
    }

    public class CancelChequeLeafRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ChequeBookInventoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string BookReference { get; set; } = string.Empty;
        public string BranchId { get; set; } = string.Empty;
        public string SeriesPrefix { get; set; } = string.Empty;
        public long StartSerialNumber { get; set; }
        public long EndSerialNumber { get; set; }
        public int LeafCount { get; set; }
        public int AvailableLeafCount { get; set; }
        public int UsedLeafCount { get; set; }
        public int CancelledLeafCount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public DateTime? IssuedAt { get; set; }
        public string IssuedBy { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public List<ChequeBookLeafDto> Leaves { get; set; } = new();
    }

    public class ChequeBookLeafDto
    {
        public string Id { get; set; } = string.Empty;
        public long SerialNumber { get; set; }
        public string ChequeNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string UsedTransactionId { get; set; } = string.Empty;
        public DateTime? UsedAt { get; set; }
        public string CancelReason { get; set; } = string.Empty;
    }
}
