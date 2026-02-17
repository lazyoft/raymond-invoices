using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor asks to issue a specific draft invoice
public record IssueInvoiceRequest(Guid InvoiceId, ActorContext Actor);

// Response: the issued invoice with assigned number and recalculated totals
public record IssueInvoiceResponse(Invoice Invoice, string InvoiceNumber);

/// <summary>
/// Issues a draft invoice: validates status transition, recalculates totals,
/// assigns a progressive invoice number, and transitions status to Issued.
/// Art. 21 DPR 633/72 - Fatturazione delle operazioni.
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class IssueInvoice : IUseCase<IssueInvoiceRequest, IssueInvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IInvoiceNumberingService _numberingService;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<IssueInvoice> _logger;

    public IssueInvoice(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceNumberingService numberingService,
        IInvoiceCalculationService calculationService,
        ILogger<IssueInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _numberingService = numberingService;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<IssueInvoiceResponse> Execute(IssueInvoiceRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested issuance of invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (invoice, client, lastNumber) = await GetInvoiceWithDependencies(request);

        // Phase 2: Validate
        ValidateInvoiceExists(invoice, request);
        ValidateCanTransitionToIssued(invoice!, request);

        // Phase 3: Execute
        var response = await PerformIssuance(invoice!, client, lastNumber, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? Invoice, Client? Client, string? LastNumber)>
        GetInvoiceWithDependencies(IssueInvoiceRequest request)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId);

        Client? client = null;
        string? lastNumber = null;

        if (invoice != null)
        {
            client = await _clientRepository.GetByIdAsync(invoice.ClientId);
            lastNumber = await _invoiceRepository.GetLastInvoiceNumberAsync();
        }

        return (invoice, client, lastNumber);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? invoice, IssueInvoiceRequest request)
    {
        if (invoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested issuance of non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateCanTransitionToIssued(Invoice invoice, IssueInvoiceRequest request)
    {
        if (!InvoiceValidator.CanTransitionTo(invoice.Status, InvoiceStatus.Issued))
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted forbidden transition {From} -> Issued on invoice {InvoiceId}",
                request.Actor.UserId, invoice.Status, request.InvoiceId);

            throw new ForbiddenOperationException(
                "IssueInvoice",
                "Fattura",
                $"Cannot transition from {invoice.Status} to Issued");
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<IssueInvoiceResponse> PerformIssuance(
        Invoice invoice, Client? client, string? lastNumber, IssueInvoiceRequest request)
    {
        // Attach client for ritenuta/split-payment calculation
        invoice.Client = client;

        // Recalculate totals before issuing (Bug #8 fix)
        _calculationService.CalculateInvoiceTotals(invoice);

        // Generate progressive invoice number
        invoice.InvoiceNumber = _numberingService.GenerateNextInvoiceNumber(lastNumber);

        // Transition status
        invoice.Status = InvoiceStatus.Issued;

        // Persist
        var updated = await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} issued as {InvoiceNumber} by actor {ActorId}. TotalDue: {TotalDue}",
            invoice.Id, invoice.InvoiceNumber, request.Actor.UserId, invoice.TotalDue);

        return new IssueInvoiceResponse(updated!, invoice.InvoiceNumber);
    }
}
