using BankInsight.API.Services;
using FluentAssertions;
using HybridTransfer.Api.Contracts;
using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.Services;
using HybridApprovalService = HybridTransfer.Application.Services.ApprovalService;
using HybridPostingEngine = HybridTransfer.Application.Services.PostingEngine;
using HybridTransfer.Domain.Common;
using HybridTransfer.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace BankInsight.IntegrationTests.Services;

public class BankTransferLifecycleServiceTests
{
    [Fact]
    public async Task SubmitAndVerifyBankPayout_PostsPendingThenSettlement()
    {
        var transferRepository = new InMemoryTransferOrderRepository();
        var walletRepository = new InMemoryWalletProjectionRepository();
        var journalRepository = new InMemoryJournalRepository();
        var auditRepository = new InMemoryAuditEventRepository();
        var approvalRepository = new InMemoryApprovalRequestRepository();
        var alertRepository = new InMemoryAlertRepository();
        var reconciliationRepository = new InMemoryReconciliationRepository();
        var auditTrail = new AuditTrailService(auditRepository);
        var reconciliationService = new ReconciliationService(reconciliationRepository);
        var statusProvider = new StubBankProvider(verifyStatus: "success");
        var payoutOrchestrator = CreatePayoutOrchestrator(transferRepository, walletRepository, approvalRepository, alertRepository, statusProvider);
        var ledgerService = CreateLedgerService(journalRepository, walletRepository, transferRepository, auditTrail);
        var statusService = new ProviderTransferStatusService(transferRepository, statusProvider, auditTrail);
        var lifecycleService = new BankTransferLifecycleService(
            payoutOrchestrator,
            statusService,
            ledgerService,
            transferRepository,
            walletRepository,
            journalRepository,
            reconciliationService,
            auditTrail,
            Options.Create(new FintechLedgerOptions()));

        var submit = await lifecycleService.SubmitBankPayoutAsync(
            new BankTransferRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), "057", "0123456789", 100m, "GHS", "Acme Ghana", "Vendor settlement"),
            "customer",
            "idem-bank-1",
            CancellationToken.None);

        submit.Status.Should().Be(TransferStatus.PendingSettlement.ToString());
        var transfer = await transferRepository.GetByIdAsync(submit.TransferId, CancellationToken.None);
        transfer.Should().NotBeNull();
        transfer!.Status.Should().Be(TransferStatus.PendingSettlement);
        (await walletRepository.GetAvailableBalanceAsync(transfer.SourceWalletId, CancellationToken.None)).Should().Be(4900m);
        (await journalRepository.GetByTransferOrderIdAsync(transfer.Id, CancellationToken.None)).Should().HaveCount(1);

        var verified = await lifecycleService.VerifyBankTransferAsync(transfer.PartnerReference!, "ops-user", CancellationToken.None);
        verified.TransferStatus.Should().Be(TransferStatus.Posted.ToString());
        (await journalRepository.GetByTransferOrderIdAsync(transfer.Id, CancellationToken.None)).Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitAndApplyFailedCallback_PostsReversalAndRestoresWalletBalance()
    {
        var transferRepository = new InMemoryTransferOrderRepository();
        var walletRepository = new InMemoryWalletProjectionRepository();
        var journalRepository = new InMemoryJournalRepository();
        var auditRepository = new InMemoryAuditEventRepository();
        var approvalRepository = new InMemoryApprovalRequestRepository();
        var alertRepository = new InMemoryAlertRepository();
        var reconciliationRepository = new InMemoryReconciliationRepository();
        var auditTrail = new AuditTrailService(auditRepository);
        var reconciliationService = new ReconciliationService(reconciliationRepository);
        var statusProvider = new StubBankProvider(verifyStatus: "success");
        var payoutOrchestrator = CreatePayoutOrchestrator(transferRepository, walletRepository, approvalRepository, alertRepository, statusProvider);
        var ledgerService = CreateLedgerService(journalRepository, walletRepository, transferRepository, auditTrail);
        var statusService = new ProviderTransferStatusService(transferRepository, statusProvider, auditTrail);
        var lifecycleService = new BankTransferLifecycleService(
            payoutOrchestrator,
            statusService,
            ledgerService,
            transferRepository,
            walletRepository,
            journalRepository,
            reconciliationService,
            auditTrail,
            Options.Create(new FintechLedgerOptions()));

        var submit = await lifecycleService.SubmitBankPayoutAsync(
            new BankTransferRequest(Guid.Parse("11111111-1111-1111-1111-111111111111"), "057", "0123456789", 100m, "GHS", "Acme Ghana", "Vendor settlement"),
            "customer",
            "idem-bank-2",
            CancellationToken.None);

        var transfer = await transferRepository.GetByIdAsync(submit.TransferId, CancellationToken.None);
        transfer.Should().NotBeNull();

        var sync = await lifecycleService.ApplyBankTransferCallbackAsync(transfer!.PartnerReference!, "failed", "bank rejected payout", "webhook:paystack", CancellationToken.None);
        sync.TransferStatus.Should().Be(TransferStatus.Reversed.ToString());
        (await walletRepository.GetAvailableBalanceAsync(transfer.SourceWalletId, CancellationToken.None)).Should().Be(5000m);
        (await journalRepository.GetByTransferOrderIdAsync(transfer.Id, CancellationToken.None)).Should().HaveCount(2);
    }

    private static PayoutOrchestrator CreatePayoutOrchestrator(
        InMemoryTransferOrderRepository transferRepository,
        InMemoryWalletProjectionRepository walletRepository,
        InMemoryApprovalRequestRepository approvalRepository,
        InMemoryAlertRepository alertRepository,
        StubBankProvider bankProvider)
    {
        var mobileProvider = new StubMobileMoneyProvider();
        var transferExecutionService = new TransferExecutionService(transferRepository, mobileProvider, bankProvider);
        var approvalService = new HybridApprovalService(approvalRepository, transferRepository, transferExecutionService);
        var riskService = new RiskAssessmentService(alertRepository);
        return new PayoutOrchestrator(transferRepository, walletRepository, mobileProvider, bankProvider, riskService, approvalService);
    }

    private static LedgerApplicationService CreateLedgerService(
        InMemoryJournalRepository journalRepository,
        InMemoryWalletProjectionRepository walletRepository,
        InMemoryTransferOrderRepository transferRepository,
        AuditTrailService auditTrail)
    {
        return new LedgerApplicationService(
            new HybridPostingEngine(),
            journalRepository,
            walletRepository,
            transferRepository,
            new InMemoryTransactionManager(),
            new TransferPostingPolicyService(),
            auditTrail);
    }

    private sealed class StubMobileMoneyProvider : IMobileMoneyProvider
    {
        public Task<ProviderTransferResult> InitiatePayoutAsync(MobileMoneyPayoutInstruction instruction, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderTransferResult(true, $"MOMO-{instruction.TransferId:N}", "Submitted"));
    }

    private sealed class StubBankProvider : IBankTransferProvider, IProviderTransferStatusProvider
    {
        private readonly string _verifyStatus;

        public StubBankProvider(string verifyStatus)
        {
            _verifyStatus = verifyStatus;
        }

        public Task<ProviderTransferResult> InitiatePayoutAsync(BankPayoutInstruction instruction, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderTransferResult(true, $"BANK-{instruction.TransferId:N}", "Submitted"));

        public Task<ProviderTransferStatusResult> GetBankTransferStatusAsync(string providerReference, CancellationToken cancellationToken)
            => Task.FromResult(new ProviderTransferStatusResult(true, providerReference, _verifyStatus, null));
    }
}
