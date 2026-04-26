using System.Text.Json;
using BankInsight.API.Data;
using BankInsight.API.DTOs;
using BankInsight.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace BankInsight.API.Services;

public interface IInternalCreditScoringService
{
    Task<InternalCreditScoreResult> ScoreCustomerAsync(string customerId, string? loanId = null, CancellationToken cancellationToken = default);
    Task<CreditScoringModelStatusDto> GetModelStatusAsync(CancellationToken cancellationToken = default);
}

public sealed class InternalCreditScoreResult
{
    public int Score { get; set; }
    public decimal ProbabilityGood { get; set; }
    public string RiskBand { get; set; } = "UNKNOWN";
    public string RiskGrade { get; set; } = "UNKNOWN";
    public string Decision { get; set; } = "REVIEW";
    public string Recommendation { get; set; } = "Manual review";
    public string ModelVersion { get; set; } = "ml-credit-v1";
    public int TrainingSampleCount { get; set; }
    public int PositiveSampleCount { get; set; }
    public int NegativeSampleCount { get; set; }
    public DateTime TrainedAtUtc { get; set; }
    public Dictionary<string, decimal> FeatureSummary { get; set; } = new();
}

public sealed class InternalCreditScoringService : IInternalCreditScoringService
{
    private const string ModelVersion = "ml-credit-v1";
    private static readonly TimeSpan ModelRefreshInterval = TimeSpan.FromHours(6);
    private static readonly SemaphoreSlim ModelLock = new(1, 1);
    private static readonly MLContext MlContext = new(seed: 42);
    private static ModelState? _cachedModel;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<InternalCreditScoringService> _logger;

    public InternalCreditScoringService(ApplicationDbContext context, ILogger<InternalCreditScoringService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<InternalCreditScoreResult> ScoreCustomerAsync(string customerId, string? loanId = null, CancellationToken cancellationToken = default)
    {
        var feature = await BuildFeatureVectorAsync(customerId, cancellationToken);
        var model = await EnsureModelAsync(cancellationToken);

        if (model.Transformer == null)
        {
            var heuristic = ScoreHeuristically(feature, model);
            await PersistAssessmentAsync(customerId, loanId, heuristic, cancellationToken);
            return heuristic;
        }

        var predictionEngine = MlContext.Model.CreatePredictionEngine<CreditTrainingRow, CreditPrediction>(model.Transformer);
        var prediction = predictionEngine.Predict(feature.ToTrainingRow(label: false));
        var probability = prediction.ProbabilityGood;
        var score = ScaleScore(probability);
        var result = new InternalCreditScoreResult
        {
            Score = score,
            ProbabilityGood = Math.Round((decimal)probability, 6),
            RiskBand = ResolveRiskBand(score),
            RiskGrade = ResolveRiskGrade(score),
            Decision = ResolveDecision(score),
            Recommendation = BuildRecommendation(score),
            ModelVersion = ModelVersion,
            TrainingSampleCount = model.TrainingSampleCount,
            PositiveSampleCount = model.PositiveSampleCount,
            NegativeSampleCount = model.NegativeSampleCount,
            TrainedAtUtc = model.TrainedAtUtc,
            FeatureSummary = feature.ToSummary()
        };

        await PersistAssessmentAsync(customerId, loanId, result, cancellationToken);
        return result;
    }

    public async Task<CreditScoringModelStatusDto> GetModelStatusAsync(CancellationToken cancellationToken = default)
    {
        var model = await EnsureModelAsync(cancellationToken);
        return new CreditScoringModelStatusDto
        {
            ModelReady = model.Transformer != null,
            ModelVersion = ModelVersion,
            TrainedAtUtc = model.TrainedAtUtc == default ? null : model.TrainedAtUtc,
            TrainingSampleCount = model.TrainingSampleCount,
            PositiveSampleCount = model.PositiveSampleCount,
            NegativeSampleCount = model.NegativeSampleCount,
            HeuristicFallbackEnabled = model.Transformer == null,
            StatusMessage = model.Transformer == null
                ? "ML model training data is insufficient; heuristic scoring is active."
                : "ML credit scoring model is trained and ready."
        };
    }

    private async Task<ModelState> EnsureModelAsync(CancellationToken cancellationToken)
    {
        if (_cachedModel != null && DateTime.UtcNow - _cachedModel.TrainedAtUtc < ModelRefreshInterval)
        {
            return _cachedModel;
        }

        await ModelLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedModel != null && DateTime.UtcNow - _cachedModel.TrainedAtUtc < ModelRefreshInterval)
            {
                return _cachedModel;
            }

            var dataset = await BuildTrainingDatasetAsync(cancellationToken);
            if (dataset.Count < 25 || dataset.All(x => x.Label == true) || dataset.All(x => x.Label == false))
            {
                _cachedModel = new ModelState
                {
                    Transformer = null,
                    TrainedAtUtc = DateTime.UtcNow,
                    TrainingSampleCount = dataset.Count,
                    PositiveSampleCount = dataset.Count(x => x.Label == true),
                    NegativeSampleCount = dataset.Count(x => x.Label == false)
                };
                return _cachedModel;
            }

            var dataView = MlContext.Data.LoadFromEnumerable(dataset.Select(d => d.ToTrainingRow(label: d.Label!.Value)));
            var pipeline = MlContext.Transforms.Concatenate("Features",
                    nameof(CreditTrainingRow.DepositCount90d),
                    nameof(CreditTrainingRow.WithdrawalCount90d),
                    nameof(CreditTrainingRow.DepositAmount90d),
                    nameof(CreditTrainingRow.WithdrawalAmount90d),
                    nameof(CreditTrainingRow.NetCashFlow90d),
                    nameof(CreditTrainingRow.DepositWithdrawalRatio90d),
                    nameof(CreditTrainingRow.AccountTenureDays),
                    nameof(CreditTrainingRow.ActiveAccountCount),
                    nameof(CreditTrainingRow.AverageAvailableBalance),
                    nameof(CreditTrainingRow.TotalOutstandingLoanBalance),
                    nameof(CreditTrainingRow.ActiveLoanCount),
                    nameof(CreditTrainingRow.ClosedLoanCount),
                    nameof(CreditTrainingRow.WrittenOffLoanCount),
                    nameof(CreditTrainingRow.LoanRepaymentCount365d),
                    nameof(CreditTrainingRow.RepaymentAmount365d),
                    nameof(CreditTrainingRow.OnTimeRepaymentRatio),
                    nameof(CreditTrainingRow.AverageDaysPastDue),
                    nameof(CreditTrainingRow.MaxDaysPastDue),
                    nameof(CreditTrainingRow.CurrentSavingsToExposureRatio),
                    nameof(CreditTrainingRow.RecentTransactionCount90d))
                .Append(MlContext.BinaryClassification.Trainers.FastTree(
                    labelColumnName: nameof(CreditTrainingRow.Label),
                    featureColumnName: "Features",
                    numberOfLeaves: 20,
                    numberOfTrees: 150,
                    minimumExampleCountPerLeaf: 5));

            var model = pipeline.Fit(dataView);
            _cachedModel = new ModelState
            {
                Transformer = model,
                TrainedAtUtc = DateTime.UtcNow,
                TrainingSampleCount = dataset.Count,
                PositiveSampleCount = dataset.Count(x => x.Label == true),
                NegativeSampleCount = dataset.Count(x => x.Label == false)
            };
            return _cachedModel;
        }
        finally
        {
            ModelLock.Release();
        }
    }

    private async Task<List<CustomerCreditFeatures>> BuildTrainingDatasetAsync(CancellationToken cancellationToken)
    {
        var snapshots = await BuildCustomerFeatureSnapshotsAsync(cancellationToken);
        return snapshots
            .Where(x => x.Label.HasValue)
            .ToList();
    }

    private async Task<CustomerCreditFeatures> BuildFeatureVectorAsync(string customerId, CancellationToken cancellationToken)
    {
        var snapshots = await BuildCustomerFeatureSnapshotsAsync(cancellationToken, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { customerId });
        return snapshots.FirstOrDefault(x => string.Equals(x.CustomerId, customerId, StringComparison.OrdinalIgnoreCase))
            ?? new CustomerCreditFeatures(customerId);
    }

    private async Task<List<CustomerCreditFeatures>> BuildCustomerFeatureSnapshotsAsync(CancellationToken cancellationToken, HashSet<string>? limitToCustomerIds = null)
    {
        var now = DateTime.UtcNow;
        var since90 = now.AddDays(-90);
        var since365 = now.AddDays(-365);

        var loanRows = await _context.Loans
            .AsNoTracking()
            .Where(l => l.CustomerId != null && (limitToCustomerIds == null || limitToCustomerIds.Contains(l.CustomerId)))
            .Select(l => new
            {
                CustomerId = l.CustomerId!,
                l.Id,
                l.Status,
                l.Principal,
                OutstandingBalance = l.OutstandingBalance ?? 0m,
                DisbursedAt = l.DisbursementDate,
                l.ParBucket
            })
            .ToListAsync(cancellationToken);

        var customerIds = loanRows.Select(l => l.CustomerId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (limitToCustomerIds != null)
        {
            foreach (var customerId in limitToCustomerIds)
            {
                customerIds.Add(customerId);
            }
        }

        var accountRows = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId != null && customerIds.Contains(a.CustomerId))
            .Select(a => new
            {
                CustomerId = a.CustomerId!,
                a.Status,
                a.Balance,
                a.LienAmount,
                a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var transactionRows = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId != null && t.Date >= since365)
            .Join(
                _context.Accounts.AsNoTracking().Where(a => a.CustomerId != null && customerIds.Contains(a.CustomerId)),
                t => t.AccountId,
                a => a.Id,
                (t, a) => new
                {
                    CustomerId = a.CustomerId!,
                    t.Type,
                    t.Amount,
                    t.Date
                })
            .ToListAsync(cancellationToken);

        var repaymentRows = await _context.LoanRepayments
            .AsNoTracking()
            .Join(
                _context.Loans.AsNoTracking().Where(l => l.CustomerId != null && customerIds.Contains(l.CustomerId)),
                r => r.LoanId,
                l => l.Id,
                (r, l) => new
                {
                    CustomerId = l.CustomerId!,
                    r.Amount,
                    r.RepaymentDate,
                    r.IsReversal
                })
            .Where(x => !x.IsReversal && x.RepaymentDate >= since365)
            .ToListAsync(cancellationToken);

        var scheduleRows = await _context.LoanSchedules
            .AsNoTracking()
            .Join(
                _context.Loans.AsNoTracking().Where(l => l.CustomerId != null && customerIds.Contains(l.CustomerId)),
                s => s.LoanId,
                l => l.Id,
                (s, l) => new
                {
                    CustomerId = l.CustomerId!,
                    s.Status,
                    s.DueDate,
                    s.PaidDate,
                    Balance = s.Balance ?? 0m,
                    Total = s.Total ?? 0m
                })
            .ToListAsync(cancellationToken);

        var snapshots = customerIds.ToDictionary(id => id, id => new CustomerCreditFeatures(id), StringComparer.OrdinalIgnoreCase);

        foreach (var account in accountRows)
        {
            var snapshot = snapshots[account.CustomerId];
            snapshot.ActiveAccountCount += string.Equals(account.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            snapshot.TotalAvailableBalance += (float)Math.Max(0m, account.Balance - account.LienAmount);
            snapshot.AccountTenureDays = Math.Max(snapshot.AccountTenureDays, (float)Math.Max(0, (now - account.CreatedAt).TotalDays));
        }

        foreach (var tx in transactionRows)
        {
            var snapshot = snapshots[tx.CustomerId];
            if (tx.Date >= since90)
            {
                snapshot.RecentTransactionCount90d += 1;
            }

            if (IsDeposit(tx.Type))
            {
                if (tx.Date >= since90)
                {
                    snapshot.DepositCount90d += 1;
                    snapshot.DepositAmount90d += (float)tx.Amount;
                }
            }
            else if (IsWithdrawal(tx.Type))
            {
                if (tx.Date >= since90)
                {
                    snapshot.WithdrawalCount90d += 1;
                    snapshot.WithdrawalAmount90d += (float)tx.Amount;
                }
            }
        }

        foreach (var repayment in repaymentRows)
        {
            var snapshot = snapshots[repayment.CustomerId];
            snapshot.LoanRepaymentCount365d += 1;
            snapshot.RepaymentAmount365d += (float)repayment.Amount;
        }

        foreach (var loan in loanRows)
        {
            var snapshot = snapshots[loan.CustomerId];
            snapshot.TotalOutstandingLoanBalance += (float)loan.OutstandingBalance;

            if (string.Equals(loan.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                snapshot.ActiveLoanCount += 1;
            }
            else if (string.Equals(loan.Status, "CLOSED", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(loan.Status, "RECOVERED", StringComparison.OrdinalIgnoreCase))
            {
                snapshot.ClosedLoanCount += 1;
            }
            else if (string.Equals(loan.Status, "WRITTEN_OFF", StringComparison.OrdinalIgnoreCase))
            {
                snapshot.WrittenOffLoanCount += 1;
            }
        }

        foreach (var schedule in scheduleRows)
        {
            var snapshot = snapshots[schedule.CustomerId];
            var dueDate = schedule.DueDate?.ToDateTime(TimeOnly.MinValue) ?? now;
            var isPaid = string.Equals(schedule.Status, "PAID", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(schedule.Status, "SETTLED", StringComparison.OrdinalIgnoreCase);

            if (!isPaid && dueDate < now && schedule.Balance > 0m)
            {
                var daysPastDue = Math.Max(0, (int)(now.Date - dueDate.Date).TotalDays);
                snapshot.MaxDaysPastDue = Math.Max(snapshot.MaxDaysPastDue, daysPastDue);
                snapshot.TotalDaysPastDue += daysPastDue;
                snapshot.OverdueInstallmentCount += 1;
            }

            if (isPaid)
            {
                if (schedule.PaidDate.HasValue && schedule.DueDate.HasValue && schedule.PaidDate.Value <= schedule.DueDate.Value)
                {
                    snapshot.OnTimeInstallmentCount += 1;
                }

                snapshot.PaidInstallmentCount += 1;
            }

            if (schedule.Total > 0m)
            {
                snapshot.ScheduledInstallmentCount += 1;
            }
        }

        foreach (var snapshot in snapshots.Values)
        {
            snapshot.FinalizeDerivedMetrics();
            snapshot.Label = DetermineTrainingLabel(snapshot);
        }

        return snapshots.Values.ToList();
    }

    private async Task PersistAssessmentAsync(string customerId, string? loanId, InternalCreditScoreResult result, CancellationToken cancellationToken)
    {
        _context.InternalCreditScoreAssessments.Add(new InternalCreditScoreAssessment
        {
            CustomerId = customerId,
            LoanId = loanId,
            Score = result.Score,
            ProbabilityGood = result.ProbabilityGood,
            RiskBand = result.RiskBand,
            RiskGrade = result.RiskGrade,
            Decision = result.Decision,
            Recommendation = result.Recommendation,
            ModelVersion = result.ModelVersion,
            TrainingSampleCount = result.TrainingSampleCount,
            FeaturePayload = JsonSerializer.Serialize(result.FeatureSummary),
            CheckedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static InternalCreditScoreResult ScoreHeuristically(CustomerCreditFeatures feature, ModelState model)
    {
        var raw = 560m;
        raw += Math.Min(120m, (decimal)feature.DepositAmount90d / 500m);
        raw += Math.Min(60m, (decimal)feature.TotalAvailableBalance / 1000m);
        raw += Math.Min(80m, (decimal)feature.OnTimeRepaymentRatio * 100m);
        raw -= Math.Min(140m, (decimal)feature.MaxDaysPastDue * 1.2m);
        raw -= Math.Min(120m, (decimal)feature.WrittenOffLoanCount * 80m);
        raw -= Math.Min(80m, (decimal)feature.TotalOutstandingLoanBalance / 5000m * 20m);
        raw += Math.Min(40m, (decimal)feature.ActiveAccountCount * 10m);

        var score = Math.Clamp((int)Math.Round(raw, MidpointRounding.AwayFromZero), 300, 850);
        var probability = Math.Clamp((score - 300m) / 550m, 0m, 1m);

        return new InternalCreditScoreResult
        {
            Score = score,
            ProbabilityGood = Math.Round(probability, 6),
            RiskBand = ResolveRiskBand(score),
            RiskGrade = ResolveRiskGrade(score),
            Decision = ResolveDecision(score),
            Recommendation = BuildRecommendation(score),
            ModelVersion = ModelVersion,
            TrainingSampleCount = model.TrainingSampleCount,
            PositiveSampleCount = model.PositiveSampleCount,
            NegativeSampleCount = model.NegativeSampleCount,
            TrainedAtUtc = model.TrainedAtUtc == default ? DateTime.UtcNow : model.TrainedAtUtc,
            FeatureSummary = feature.ToSummary()
        };
    }

    private static bool? DetermineTrainingLabel(CustomerCreditFeatures feature)
    {
        var hasLoanHistory = feature.ActiveLoanCount + feature.ClosedLoanCount + feature.WrittenOffLoanCount > 0;
        if (!hasLoanHistory)
        {
            return null;
        }

        var severeNegative = feature.WrittenOffLoanCount > 0 || feature.MaxDaysPastDue >= 90;
        var strongPositive = feature.WrittenOffLoanCount == 0
                             && feature.MaxDaysPastDue <= 30
                             && feature.OnTimeRepaymentRatio >= 0.60f
                             && feature.RepaymentAmount365d > 0;

        if (severeNegative)
        {
            return false;
        }

        if (strongPositive)
        {
            return true;
        }

        return null;
    }

    private static bool IsDeposit(string? transactionType)
    {
        var normalized = transactionType?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Contains("DEPOSIT")
            || normalized.Contains("CREDIT")
            || normalized.Contains("LODG")
            || normalized.Contains("INWARD");
    }

    private static bool IsWithdrawal(string? transactionType)
    {
        var normalized = transactionType?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.Contains("WITHDRAW")
            || normalized.Contains("DEBIT")
            || normalized.Contains("TRANSFER")
            || normalized.Contains("PAYMENT")
            || normalized.Contains("FEE")
            || normalized.Contains("CHARGE");
    }

    private static int ScaleScore(float probability)
    {
        var scaled = 300 + (probability * 550f);
        return Math.Clamp((int)Math.Round(scaled, MidpointRounding.AwayFromZero), 300, 850);
    }

    private static string ResolveRiskBand(int score) => score switch
    {
        >= 760 => "LOW",
        >= 660 => "MEDIUM",
        >= 580 => "ELEVATED",
        _ => "HIGH"
    };

    private static string ResolveRiskGrade(int score) => score switch
    {
        >= 800 => "A",
        >= 720 => "B",
        >= 640 => "C",
        >= 560 => "D",
        _ => "E"
    };

    private static string ResolveDecision(int score) => score switch
    {
        >= 700 => "PASS",
        >= 600 => "REVIEW",
        _ => "FAIL"
    };

    private static string BuildRecommendation(int score) => score switch
    {
        >= 700 => "Eligible for standard credit processing.",
        >= 600 => "Proceed with enhanced review, collateral, or tighter limit.",
        _ => "Decline or require substantial mitigants before approval."
    };

    private sealed class ModelState
    {
        public ITransformer? Transformer { get; set; }
        public DateTime TrainedAtUtc { get; set; }
        public int TrainingSampleCount { get; set; }
        public int PositiveSampleCount { get; set; }
        public int NegativeSampleCount { get; set; }
    }

    private sealed record CustomerCreditFeatures(string CustomerId)
    {
        public float DepositCount90d { get; set; }
        public float WithdrawalCount90d { get; set; }
        public float DepositAmount90d { get; set; }
        public float WithdrawalAmount90d { get; set; }
        public float NetCashFlow90d { get; set; }
        public float DepositWithdrawalRatio90d { get; set; }
        public float AccountTenureDays { get; set; }
        public float ActiveAccountCount { get; set; }
        public float AverageAvailableBalance { get; set; }
        public float TotalOutstandingLoanBalance { get; set; }
        public float ActiveLoanCount { get; set; }
        public float ClosedLoanCount { get; set; }
        public float WrittenOffLoanCount { get; set; }
        public float LoanRepaymentCount365d { get; set; }
        public float RepaymentAmount365d { get; set; }
        public float OnTimeRepaymentRatio { get; set; }
        public float AverageDaysPastDue { get; set; }
        public float MaxDaysPastDue { get; set; }
        public float CurrentSavingsToExposureRatio { get; set; }
        public float RecentTransactionCount90d { get; set; }
        public float TotalAvailableBalance { get; set; }
        public float ScheduledInstallmentCount { get; set; }
        public float PaidInstallmentCount { get; set; }
        public float OnTimeInstallmentCount { get; set; }
        public float OverdueInstallmentCount { get; set; }
        public float TotalDaysPastDue { get; set; }
        public bool? Label { get; set; }

        public void FinalizeDerivedMetrics()
        {
            NetCashFlow90d = DepositAmount90d - WithdrawalAmount90d;
            DepositWithdrawalRatio90d = WithdrawalAmount90d <= 0 ? DepositAmount90d : DepositAmount90d / Math.Max(WithdrawalAmount90d, 1f);
            AverageAvailableBalance = ActiveAccountCount <= 0 ? TotalAvailableBalance : TotalAvailableBalance / Math.Max(1f, ActiveAccountCount);
            OnTimeRepaymentRatio = PaidInstallmentCount <= 0 ? 0f : OnTimeInstallmentCount / Math.Max(1f, PaidInstallmentCount);
            AverageDaysPastDue = OverdueInstallmentCount <= 0 ? 0f : TotalDaysPastDue / Math.Max(1f, OverdueInstallmentCount);
            CurrentSavingsToExposureRatio = TotalOutstandingLoanBalance <= 0 ? AverageAvailableBalance : AverageAvailableBalance / Math.Max(1f, TotalOutstandingLoanBalance);
        }

        public CreditTrainingRow ToTrainingRow(bool label)
        {
            return new CreditTrainingRow
            {
                DepositCount90d = DepositCount90d,
                WithdrawalCount90d = WithdrawalCount90d,
                DepositAmount90d = DepositAmount90d,
                WithdrawalAmount90d = WithdrawalAmount90d,
                NetCashFlow90d = NetCashFlow90d,
                DepositWithdrawalRatio90d = DepositWithdrawalRatio90d,
                AccountTenureDays = AccountTenureDays,
                ActiveAccountCount = ActiveAccountCount,
                AverageAvailableBalance = AverageAvailableBalance,
                TotalOutstandingLoanBalance = TotalOutstandingLoanBalance,
                ActiveLoanCount = ActiveLoanCount,
                ClosedLoanCount = ClosedLoanCount,
                WrittenOffLoanCount = WrittenOffLoanCount,
                LoanRepaymentCount365d = LoanRepaymentCount365d,
                RepaymentAmount365d = RepaymentAmount365d,
                OnTimeRepaymentRatio = OnTimeRepaymentRatio,
                AverageDaysPastDue = AverageDaysPastDue,
                MaxDaysPastDue = MaxDaysPastDue,
                CurrentSavingsToExposureRatio = CurrentSavingsToExposureRatio,
                RecentTransactionCount90d = RecentTransactionCount90d,
                Label = label
            };
        }

        public Dictionary<string, decimal> ToSummary()
        {
            return new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                ["depositCount90d"] = (decimal)DepositCount90d,
                ["withdrawalCount90d"] = (decimal)WithdrawalCount90d,
                ["depositAmount90d"] = (decimal)DepositAmount90d,
                ["withdrawalAmount90d"] = (decimal)WithdrawalAmount90d,
                ["netCashFlow90d"] = (decimal)NetCashFlow90d,
                ["averageAvailableBalance"] = (decimal)AverageAvailableBalance,
                ["totalOutstandingLoanBalance"] = (decimal)TotalOutstandingLoanBalance,
                ["activeLoanCount"] = (decimal)ActiveLoanCount,
                ["closedLoanCount"] = (decimal)ClosedLoanCount,
                ["writtenOffLoanCount"] = (decimal)WrittenOffLoanCount,
                ["onTimeRepaymentRatio"] = (decimal)OnTimeRepaymentRatio,
                ["averageDaysPastDue"] = (decimal)AverageDaysPastDue,
                ["maxDaysPastDue"] = (decimal)MaxDaysPastDue,
                ["currentSavingsToExposureRatio"] = (decimal)CurrentSavingsToExposureRatio,
                ["recentTransactionCount90d"] = (decimal)RecentTransactionCount90d
            };
        }
    }

    private sealed class CreditTrainingRow
    {
        public float DepositCount90d { get; set; }
        public float WithdrawalCount90d { get; set; }
        public float DepositAmount90d { get; set; }
        public float WithdrawalAmount90d { get; set; }
        public float NetCashFlow90d { get; set; }
        public float DepositWithdrawalRatio90d { get; set; }
        public float AccountTenureDays { get; set; }
        public float ActiveAccountCount { get; set; }
        public float AverageAvailableBalance { get; set; }
        public float TotalOutstandingLoanBalance { get; set; }
        public float ActiveLoanCount { get; set; }
        public float ClosedLoanCount { get; set; }
        public float WrittenOffLoanCount { get; set; }
        public float LoanRepaymentCount365d { get; set; }
        public float RepaymentAmount365d { get; set; }
        public float OnTimeRepaymentRatio { get; set; }
        public float AverageDaysPastDue { get; set; }
        public float MaxDaysPastDue { get; set; }
        public float CurrentSavingsToExposureRatio { get; set; }
        public float RecentTransactionCount90d { get; set; }

        [ColumnName("Label")]
        public bool Label { get; set; }
    }

    private sealed class CreditPrediction
    {
        public bool PredictedLabel { get; set; }

        [ColumnName("Probability")]
        public float ProbabilityGood { get; set; }

        [ColumnName("Score")]
        public float RawScore { get; set; }
    }
}
