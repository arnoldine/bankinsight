using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BankInsight.API.Services
{
    public interface IAnalyticsService
    {
        Task<CustomerSegmentationDTO> GetCustomerSegmentationAsync(DateTime asOfDate);
        Task<TransactionTrendsDTO> GetTransactionTrendsAsync(DateTime periodStart, DateTime periodEnd);
        Task<ProductAnalyticsDTO> GetProductAnalyticsAsync(DateTime asOfDate);
        Task<ChannelAnalyticsDTO> GetChannelAnalyticsAsync(DateTime periodStart, DateTime periodEnd);
        Task<StaffProductivityDTO> GetStaffProductivityAnalyticsAsync(DateTime periodStart, DateTime periodEnd);
    }

    public class AnalyticsService : IAnalyticsService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(ApplicationDbContext context, ILogger<AnalyticsService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CustomerSegmentationDTO> GetCustomerSegmentationAsync(DateTime asOfDate)
        {
            var segmentation = new CustomerSegmentationDTO
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                Segments = new List<CustomerSegmentDTO>()
            };

            // Segment by account balance
            var balanceSegments = new[] { 10_000, 100_000, 1_000_000, decimal.MaxValue };
            var segmentNames = new[] { "Inactive (<10K)", "Retail (10K-100K)", "Mid (100K-1M)", "VIP (>1M)" };

            for (int i = 0; i < balanceSegments.Length; i++)
            {
                var prevLimit = i == 0 ? 0 : balanceSegments[i - 1];
                var currentLimit = balanceSegments[i];

                var customerBalances = await _context.Accounts
                    .GroupBy(a => a.CustomerId)
                    .Select(g => new { CustomerId = g.Key, Balance = g.Sum(a => a.Balance) })
                    .ToListAsync();

                var segmentBalances = customerBalances
                    .Where(c => c.Balance >= prevLimit && 
                           (i == balanceSegments.Length - 1 || c.Balance < currentLimit))
                    .Select(c => c.CustomerId)
                    .ToList();

                var segmentCustomers = await _context.Customers
                    .Where(c => segmentBalances.Contains(c.Id))
                    .ToListAsync();

                if (segmentCustomers.Any())
                    {
                        var segmentData = segmentCustomers.Select(c => new 
                        {
                            CustomerId = c.Id,
                            Balance = customerBalances.FirstOrDefault(x => x.CustomerId == c.Id)?.Balance ?? 0
                        }).ToList();

                        segmentation.Segments.Add(new CustomerSegmentDTO
                        {
                            SegmentName = segmentNames[i],
                            CustomerCount = segmentData.Count,
                            TotalBalance = segmentData.Sum(c => c.Balance),
                            AverageBalance = segmentData.Count > 0 ? segmentData.Average(c => c.Balance) : 0,
                            AverageAge = 0,
                            ChurnRate = 0
                        });
                    }
            }

            _logger.LogInformation($"Customer segmentation analysis completed with {segmentation.Segments.Count} segments");
            return segmentation;
        }

        public async Task<TransactionTrendsDTO> GetTransactionTrendsAsync(DateTime periodStart, DateTime periodEnd)
        {
            var trends = new TransactionTrendsDTO
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedDate = DateTime.UtcNow,
                DailyTrends = new List<DailyTransactionTrendDTO>()
            };

            // Get daily transaction volumes
            var dailyData = await _context.Transactions
                .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
                .GroupBy(t => t.Date.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Count = g.Count(),
                    Volume = g.Sum(t => t.Amount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            trends.DailyTrends = dailyData.Select(d => new DailyTransactionTrendDTO
            {
                Date = d.Date,
                TransactionCount = d.Count,
                Volume = d.Volume,
                AverageAmount = d.Count > 0 ? d.Volume / d.Count : 0
            }).ToList();

            trends.TotalTransactions = trends.DailyTrends.Sum(t => t.TransactionCount);
            trends.TotalVolume = trends.DailyTrends.Sum(t => t.Volume);
            trends.AverageDailyVolume = trends.DailyTrends.Average(t => t.Volume);
            trends.PeakVolume = trends.DailyTrends.Max(t => t.Volume);

            _logger.LogInformation($"Transaction trends analyzed: {trends.TotalTransactions} transactions, Volume: {trends.TotalVolume:C}");
            return trends;
        }

        public async Task<ProductAnalyticsDTO> GetProductAnalyticsAsync(DateTime asOfDate)
        {
            var analytics = new ProductAnalyticsDTO
            {
                AsOfDate = asOfDate,
                GeneratedDate = DateTime.UtcNow,
                ProductMetrics = new List<ProductMetricDTO>()
            };

            var products = await _context.Products.ToListAsync();

            foreach (var product in products)
            {
                var accountCount = await _context.Accounts
                    .CountAsync(a => a.ProductCode == product.Id);

                var accountBalance = await _context.Accounts
                    .Where(a => a.ProductCode == product.Id)
                    .SumAsync(a => a.Balance);

                var averageBalance = accountCount > 0 ? accountBalance / accountCount : 0;

                analytics.ProductMetrics.Add(new ProductMetricDTO
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductType = product.Type,
                    AccountCount = accountCount,
                    TotalBalance = accountBalance,
                    AverageBalance = accountCount > 0 ? accountBalance / accountCount : 0,
                    InterestRate = product.InterestRate ?? 0,
                    RevenueContribution = accountCount > 0 ? (accountBalance * (product.InterestRate ?? 0) / 100) : 0
                });
            }

            analytics.TotalProducts = products.Count;
            analytics.TotalAccounts = analytics.ProductMetrics.Sum(p => p.AccountCount);
            analytics.TotalBalance = analytics.ProductMetrics.Sum(p => p.TotalBalance);

            _logger.LogInformation($"Product analytics completed for {analytics.TotalProducts} products");
            return analytics;
        }

        public async Task<ChannelAnalyticsDTO> GetChannelAnalyticsAsync(DateTime periodStart, DateTime periodEnd)
        {
            var analytics = new ChannelAnalyticsDTO
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedDate = DateTime.UtcNow,
                ChannelMetrics = new List<ChannelMetricDTO>()
            };

            // Simulate channel data (in real implementation, track channel with each transaction)
            var channels = new[] { "Branch", "Mobile App", "Web", "ATM", "API" };

            foreach (var channel in channels)
            {
                var transactionCount = await _context.Transactions
                    .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
                    .CountAsync();

                var volume = await _context.Transactions
                    .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
                    .SumAsync(t => t.Amount);

                // Distribute to channels (simplified)
                var channelShare = channels.Length;
                analytics.ChannelMetrics.Add(new ChannelMetricDTO
                {
                    ChannelName = channel,
                    TransactionCount = transactionCount / channelShare,
                    TransactionVolume = volume / channelShare,
                    PercentageOfTotal = 100 / (decimal)channelShare
                });
            }

            _logger.LogInformation($"Channel analytics completed for {channels.Length} channels");
            return analytics;
        }

        public async Task<StaffProductivityDTO> GetStaffProductivityAnalyticsAsync(DateTime periodStart, DateTime periodEnd)
        {
            var analytics = new StaffProductivityDTO
            {
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                GeneratedDate = DateTime.UtcNow,
                StaffMetrics = new List<StaffMetricDTO>()
            };

            var staff = await _context.Staff
                .ToListAsync();

            foreach (var staffMember in staff)
            {
                // Note: Loan entity doesn't have OriginatedByStaffId, so we cannot track loans by originating staff
                // This would require additional tracking/audit fields on Loan entity
                var loansOriginated = 0;
                var loanValue = 0m;

                if (loansOriginated > 0 || loanValue > 0)
                {
                    analytics.StaffMetrics.Add(new StaffMetricDTO
                    {
                        StaffId = staffMember.Id,
                        StaffName = staffMember.Name,
                        LoansOriginated = loansOriginated,
                        LoanValue = loanValue,
                        AverageLoanSize = loansOriginated > 0 ? loanValue / loansOriginated : 0,
                        DefaultRate = 0 // Calculate from actual defaults
                    });
                }
            }

            analytics.TotalLoansOriginated = analytics.StaffMetrics.Sum(s => s.LoansOriginated);
            analytics.TotalLoanValue = analytics.StaffMetrics.Sum(s => s.LoanValue);

            _logger.LogInformation($"Staff productivity analyzed for {analytics.StaffMetrics.Count} staff members");
            return analytics;
        }

        // Helper methods
        private decimal CalculateAverageAge(List<Entities.Customer> customers)
        {
            // Age calculation not available - Customer entity doesn't have creation timestamp
            return 0;
        }

        private decimal CalculateChurnRate(List<Entities.Customer> customers, DateTime asOfDate)
        {
            // Churn calculation would require transaction activity tracking
            return 0;
        }
    }
}
