using HybridTransfer.Application.Abstractions;
using HybridTransfer.Application.DTOs;
using HybridTransfer.Domain.Common;
using HybridTransfer.Domain.Transfers;

namespace HybridTransfer.Application.Services;

public sealed class PayoutOrchestrator
{
    private readonly ITransferOrderRepository _transferOrderRepository;
    private readonly IWalletProjectionRepository _walletProjectionRepository;
    private readonly IMobileMoneyProvider _mobileMoneyProvider;
    private readonly IBankTransferProvider _bankTransferProvider;
    private readonly RiskAssessmentService _riskAssessmentService;
    private readonly ApprovalService _approvalService;
    private readonly CurrencyPolicyService _currencyPolicyService;
    private readonly TransferRoutingPolicyService _transferRoutingPolicyService;

    public PayoutOrchestrator(
        ITransferOrderRepository transferOrderRepository,
        IWalletProjectionRepository walletProjectionRepository,
        IMobileMoneyProvider mobileMoneyProvider,
        IBankTransferProvider bankTransferProvider,
        RiskAssessmentService riskAssessmentService,
        ApprovalService approvalService,
        CurrencyPolicyService currencyPolicyService,
        TransferRoutingPolicyService transferRoutingPolicyService)
    {
        _transferOrderRepository = transferOrderRepository;
        _walletProjectionRepository = walletProjectionRepository;
        _mobileMoneyProvider = mobileMoneyProvider;
        _bankTransferProvider = bankTransferProvider;
        _riskAssessmentService = riskAssessmentService;
        _approvalService = approvalService;
        _currencyPolicyService = currencyPolicyService;
        _transferRoutingPolicyService = transferRoutingPolicyService;
    }

    public async Task<TransferResponse> CreateMobileMoneyPayoutAsync(MobileMoneyTransferRequest request, string actor, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (await _transferOrderRepository.ExistsAsync(idempotencyKey, cancellationToken))
        {
            throw new InvalidOperationException("A payout with the same idempotency key already exists.");
        }

        _transferRoutingPolicyService.EnsureFiatRailAllowed(TransferChannel.MobileMoney, request.DestinationCountryCode);
        var wallet = await _walletProjectionRepository.GetProjectionAsync(request.SourceWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Source wallet was not found.");
        _currencyPolicyService.EnsureWalletCurrencyMatches(wallet.Currency, request.Currency);

        if (wallet.AvailableBalance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient available balance.");
        }

        var transfer = new TransferOrder(
            TransferType.MobileMoneyPayout,
            TransferChannel.MobileMoney,
            "Wallet",
            request.SourceWalletId,
            $"{request.Network}:{request.MomoNumber}",
            request.Currency,
            request.DestinationCountryCode,
            request.Amount,
            actor,
            idempotencyKey);

        transfer.ApplyPricing(Math.Round(request.Amount * 0.01m, 2), null);
        var risk = await _riskAssessmentService.EvaluateTransferAsync(Guid.Empty, transfer.Id, request.Amount, request.Network, isNewDevice: request.Amount >= 1000m, cancellationToken);
        transfer.ApplyRisk(risk.RiskStatus, risk.ComplianceStatus);

        if (transfer.RiskStatus == RiskStatus.Hold)
        {
            transfer.MarkAwaitingApproval();
            await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
            await _approvalService.CreateApprovalRequestAsync(transfer.Id, "PAYOUT_RELEASE", actor, string.Join(" | ", risk.Reasons), cancellationToken);
            return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), null);
        }

        transfer.Authorize(actor);
        var providerResult = await _mobileMoneyProvider.InitiatePayoutAsync(
            new MobileMoneyPayoutInstruction(transfer.Id, request.MomoNumber, request.Network, request.Amount, request.Currency, request.Narrative ?? "HybridTransfer payout"),
            cancellationToken);

        transfer.Submit(providerResult.ProviderReference);
        await _transferOrderRepository.SaveAsync(transfer, cancellationToken);

        return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference);
    }

    public async Task<TransferResponse> CreateBankPayoutAsync(BankPayoutInstruction instruction, Guid sourceWalletId, string actor, string idempotencyKey, CancellationToken cancellationToken)
    {
        if (await _transferOrderRepository.ExistsAsync(idempotencyKey, cancellationToken))
        {
            throw new InvalidOperationException("A payout with the same idempotency key already exists.");
        }

        _transferRoutingPolicyService.EnsureFiatRailAllowed(TransferChannel.Bank, instruction.DestinationCountryCode);
        var wallet = await _walletProjectionRepository.GetProjectionAsync(sourceWalletId, cancellationToken)
            ?? throw new InvalidOperationException("Source wallet was not found.");
        _currencyPolicyService.EnsureWalletCurrencyMatches(wallet.Currency, instruction.Currency);

        if (wallet.AvailableBalance < instruction.Amount)
        {
            throw new InvalidOperationException("Insufficient available balance.");
        }

        var transfer = new TransferOrder(TransferType.BankPayout, TransferChannel.Bank, "Wallet", sourceWalletId, $"{instruction.BankCode}:{instruction.AccountNumber}", instruction.Currency, instruction.DestinationCountryCode, instruction.Amount, actor, idempotencyKey);
        transfer.ApplyPricing(5m, null);
        var risk = await _riskAssessmentService.EvaluateTransferAsync(Guid.Empty, transfer.Id, instruction.Amount, "Bank", isNewDevice: false, cancellationToken);
        transfer.ApplyRisk(risk.RiskStatus, risk.ComplianceStatus);

        if (transfer.RiskStatus == RiskStatus.Hold)
        {
            transfer.MarkAwaitingApproval();
            await _transferOrderRepository.SaveAsync(transfer, cancellationToken);
            await _approvalService.CreateApprovalRequestAsync(transfer.Id, "BANK_PAYOUT_RELEASE", actor, string.Join(" | ", risk.Reasons), cancellationToken);
            return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), null);
        }

        transfer.Authorize(actor);
        var providerResult = await _bankTransferProvider.InitiatePayoutAsync(instruction, cancellationToken);
        transfer.Submit(providerResult.ProviderReference);
        await _transferOrderRepository.SaveAsync(transfer, cancellationToken);

        return new TransferResponse(transfer.Id, transfer.Status.ToString(), transfer.RiskStatus.ToString(), transfer.ComplianceStatus.ToString(), transfer.PartnerReference);
    }
}
