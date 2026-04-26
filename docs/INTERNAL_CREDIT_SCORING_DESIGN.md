# Internal Credit Scoring Design

Last updated: 2026-04-14  
Repository root: [C:\Backup old\dev\bankinsight](C:\Backup old\dev\bankinsight)

## Purpose

This document explains how the internal credit scoring system in `BankInsight.API` works, why it was implemented, what data it uses, how it makes decisions, and how it is integrated into the lending workflow.

Primary implementation files:

- [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs)
- [LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs)
- [LoanDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\LoanDTOs.cs)
- [InternalCreditScoreAssessment.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Entities\InternalCreditScoreAssessment.cs)
- [ApplicationDbContext.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\ApplicationDbContext.cs)
- [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs)

## Why This Exists

The platform already supports external bureau-based credit checks, but external bureau data is not always:

- available
- complete
- timely
- sufficient for thin-file customers

To make lending decisions more resilient, the API now includes an internal behavioral score based on actual customer activity inside the bank:

- deposits
- withdrawals
- account balances
- transaction activity
- loan repayment behavior
- delinquency patterns
- current loan exposure

This creates a second credit signal that can:

- stand alone when bureau data is unavailable
- complement bureau data when bureau data exists
- support policy-driven `PASS`, `REVIEW`, and `FAIL` outcomes

## High-Level Design

The design is intentionally practical:

1. collect behavior from the core banking database
2. engineer a compact set of lending features
3. derive training labels from actual historical repayment outcomes
4. train an ML.NET binary classification model
5. score an individual customer using that model
6. convert model probability into a banking-friendly score and decision band
7. persist the result for audit and later review
8. combine the internal score with bureau data when available

## Runtime Entry Points

### Internal Scoring Service

The main service is [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs).

Its public interface exposes:

- `ScoreCustomerAsync`
- `GetModelStatusAsync`

### Loan Service Integration

[LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs) injects `IInternalCreditScoringService` and calls it inside the credit evaluation flow used by:

- `POST /api/loans/check-credit`
- loan approval decision logic

### API Surface

Relevant endpoints:

- `POST /api/loans/check-credit`
- `GET /api/loans/credit-scoring/status`

## Data Sources Used

The score is based entirely on internal data already captured in the bank’s core system.

### Loan Data

Used to understand exposure and prior credit performance:

- loans
- loan schedules
- loan repayments
- delinquency state
- write-off history

### Account Data

Used to understand balance and account maturity:

- active accounts
- balance
- lien amount
- account age / tenure

### Transaction Data

Used to understand inflow, outflow, and account usage:

- deposits
- withdrawals
- transfer-like outflows
- transaction counts
- transaction amounts

## Feature Engineering

The scoring service builds one feature vector per customer.

The current feature set includes:

- `DepositCount90d`
- `WithdrawalCount90d`
- `DepositAmount90d`
- `WithdrawalAmount90d`
- `NetCashFlow90d`
- `DepositWithdrawalRatio90d`
- `AccountTenureDays`
- `ActiveAccountCount`
- `AverageAvailableBalance`
- `TotalOutstandingLoanBalance`
- `ActiveLoanCount`
- `ClosedLoanCount`
- `WrittenOffLoanCount`
- `LoanRepaymentCount365d`
- `RepaymentAmount365d`
- `OnTimeRepaymentRatio`
- `AverageDaysPastDue`
- `MaxDaysPastDue`
- `CurrentSavingsToExposureRatio`
- `RecentTransactionCount90d`

### Derived Metrics

Some metrics are derived from base aggregates:

- `NetCashFlow90d = DepositAmount90d - WithdrawalAmount90d`
- `DepositWithdrawalRatio90d = deposits / withdrawals`
- `AverageAvailableBalance = total available balance / active accounts`
- `OnTimeRepaymentRatio = on-time installments / paid installments`
- `AverageDaysPastDue = total overdue days / overdue installment count`
- `CurrentSavingsToExposureRatio = available balance / outstanding loan exposure`

These derived metrics are intended to capture:

- liquidity quality
- operating discipline
- customer resilience
- debt pressure
- repayment consistency

## Training Label Strategy

There is no hand-labeled external ML dataset bundled with the system. Instead, labels are inferred from real historical credit behavior.

This logic lives in `DetermineTrainingLabel` inside [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs).

### Positive Label

A customer is considered a positive training case when:

- they have actual loan history
- they have no write-offs
- maximum days past due is low
- on-time repayment ratio is meaningfully good
- they have real repayments on record

### Negative Label

A customer is considered a negative training case when:

- they have at least one written-off loan, or
- they have severe delinquency, currently implemented as `>= 90` days past due

### Unlabeled Rows

Customers are excluded from training if:

- they have no meaningful loan history, or
- their historical outcome is too ambiguous to classify safely

This is a deliberate choice. It avoids pretending uncertain repayment behavior is a reliable label.

## Model Choice

The ML layer uses ML.NET and currently trains a binary classification model with `FastTree`.

Why this was chosen:

- works well on tabular numeric features
- easy to integrate in .NET
- suitable for structured business data
- lighter operational footprint than introducing a separate Python model stack

The pipeline:

1. concatenate numeric features into the `Features` vector
2. run a binary classifier using `FastTree`
3. predict the probability that the customer is a “good” credit outcome

The implementation sits in the `EnsureModelAsync` path of [InternalCreditScoringService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\InternalCreditScoringService.cs).

## Model Lifecycle

The model is trained in-process and cached in memory.

### Refresh Strategy

The model is refreshed every 6 hours.

Implementation details:

- a static `MLContext` is used
- a static cached model state is maintained
- a semaphore ensures one refresh at a time

This means:

- the model is not retrained on every request
- the model remains reasonably fresh
- concurrent requests do not stampede training

## Fallback Behavior

If the training dataset is too weak, the service deliberately avoids pretending that the ML model is reliable.

The service falls back when:

- fewer than 25 labeled samples exist
- all labeled samples are positive
- all labeled samples are negative

In that case, the model state is marked as heuristic fallback, and scoring is performed using deterministic banking logic instead of ML inference.

## Heuristic Scoring

The fallback heuristic still uses the same behavioral features, but scores them manually.

It starts from a base score and then:

- adds points for higher deposit inflows
- adds points for stronger balances
- adds points for stronger on-time repayment behavior
- subtracts points for days past due
- subtracts points for write-offs
- subtracts points for high outstanding exposure
- adds modest points for more active accounts

The final heuristic score is clamped into the same `300–850` range used by the ML score.

This gives operational continuity even before the bank accumulates enough repayment history for robust training.

## Score Scaling

The ML model produces a probability that the customer is a good outcome.

That probability is scaled into a banking-style score:

- minimum: `300`
- maximum: `850`

Scaling logic:

- `300 + probability * 550`

This gives a familiar numeric range for downstream decisioning and UI presentation.

## Risk Mapping

The internal score is converted into business categories.

### Risk Band

- `>= 760` → `LOW`
- `>= 660` → `MEDIUM`
- `>= 580` → `ELEVATED`
- otherwise → `HIGH`

### Risk Grade

- `>= 800` → `A`
- `>= 720` → `B`
- `>= 640` → `C`
- `>= 560` → `D`
- otherwise → `E`

### Decision

- `>= 700` → `PASS`
- `>= 600` → `REVIEW`
- otherwise → `FAIL`

### Recommendation

The recommendation text is generated from the final decision:

- `PASS` → eligible for standard processing
- `REVIEW` → enhanced review, collateral, or tighter limit
- `FAIL` → decline or require strong mitigants

## Persistence and Audit

Every internal score assessment is persisted in [InternalCreditScoreAssessment.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Entities\InternalCreditScoreAssessment.cs).

Stored fields include:

- customer ID
- optional loan ID
- score
- probability of good outcome
- risk band
- risk grade
- decision
- recommendation
- model version
- training sample count
- feature payload as JSON
- timestamp

The table is registered in [ApplicationDbContext.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\ApplicationDbContext.cs) with indexes on:

- `(CustomerId, CheckedAt)`
- `(LoanId, CheckedAt)`

That gives the platform:

- auditability
- historical score tracking
- explainability through feature payload
- future monitoring potential

## Combination with Bureau Data

The internal score does not replace bureau checks. It complements them.

In [LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs), the internal score is combined with bureau data when bureau data is available.

### Weighting

Current weighting:

- internal score: `70%`
- bureau score: `30%`

If bureau data is missing, the internal score becomes the effective score.

### Composite Decision Logic

Composite decisioning is resolved in `ResolveCompositeDecision` inside [LoanService.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Services\LoanService.cs).

The current behavior is intentionally conservative:

- if internal decision is `FAIL`, the composite result is `FAIL`
- if bureau is required by config and unavailable, the result can stay blocked/review-oriented
- otherwise, composite score thresholds drive `PASS`, `REVIEW`, or `FAIL`

### Composite Output

The API response includes:

- `InternalScore`
- `BureauScore`
- `CompositeScore`
- `ProbabilityGood`
- `RiskBand`
- `RiskGrade`
- `Decision`
- `Recommendation`
- `FeatureSummary`
- model metadata

## API Contracts

The request and response contracts are in [LoanDTOs.cs](C:\Backup old\dev\bankinsight\BankInsight.API\DTOs\LoanDTOs.cs).

### Request

`CheckCreditRequest`

Fields:

- `CustomerId`
- optional `LoanId`
- optional `ProviderName`

### Response

`CreditCheckDto`

Important output fields:

- `Score`
- `InternalScore`
- `BureauScore`
- `CompositeScore`
- `ProbabilityGood`
- `RiskBand`
- `RiskGrade`
- `Decision`
- `Recommendation`
- `ModelVersion`
- `TrainingSampleCount`
- `FeatureSummary`
- `CheckedAt`

### Model Status

`CreditScoringModelStatusDto`

Exposes:

- model readiness
- model version
- trained timestamp
- positive and negative sample counts
- whether heuristic fallback is active
- status message

## Registration and Dependency Injection

The service is registered in [Program.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Program.cs):

- `IInternalCreditScoringService`
- `InternalCreditScoringService`

This makes it available to the lending layer and any future risk or reporting components.

## Operational Characteristics

### Strengths

- uses actual internal banking behavior
- works even when bureau data is absent
- stays inside the .NET stack
- produces explainable feature summaries
- persists scoring outcomes for audit and later review
- supports gradual maturity from heuristic to ML-driven scoring

### Limits

- training labels are inferred, not manually curated
- model quality depends on repayment-history quality and volume
- the current model is not yet using champion/challenger evaluation
- there is no separate batch feature store yet
- the current implementation does not yet expose feature importance or SHAP-style explanations

## Current Decision Philosophy

This implementation is best understood as:

- a production-friendly internal behavioral score
- not a fully regulated enterprise model-risk platform

It is intended to improve decision quality immediately while still being simple enough to operate inside the existing banking stack.

## Future Enhancements

Natural next steps include:

- explicit train/validation/test evaluation metrics
- drift monitoring over time
- score calibration
- feature importance reporting
- branch, segment, and product-specific models
- early-warning monitoring for deteriorating accounts
- stronger delinquency sequence features
- alternative model comparison beyond `FastTree`
- offline retraining jobs with model persistence to storage

## Summary

The internal credit scoring system in BankInsight.API uses ML.NET to turn customer transaction behavior, deposit and withdrawal activity, balances, repayment behavior, and delinquency signals into an internal credit eligibility score.

It:

- learns from existing repayment outcomes
- produces a score from `300` to `850`
- maps that score into business risk bands and lending decisions
- persists the result for audit
- combines with bureau data when available
- falls back safely to a deterministic heuristic when data is insufficient

This gives the lending platform a practical, explainable, .NET-native behavioral scoring capability directly inside the API.
