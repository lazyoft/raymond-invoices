using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor asks to recalculate totals for a specific invoice
public record RecalculateInvoiceRequest(Guid InvoiceId, ActorContext Actor);

// Response: the updated invoice with recalculated totals
public record RecalculateInvoiceResponse(Invoice Invoice);

/// <summary>
/// Recalculates all totals for a draft invoice: imponibile, IVA, ritenuta, bollo, and total due.
/// Only draft invoices can be recalculated; issued invoices are immutable (Art. 21 DPR 633/72).
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class RecalculateInvoice : IUseCase<RecalculateInvoiceRequest, RecalculateInvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<RecalculateInvoice> _logger;

    public RecalculateInvoice(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService,
        ILogger<RecalculateInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<RecalculateInvoiceResponse> Execute(RecalculateInvoiceRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested recalculation of invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (invoice, client) = await GetInvoiceAndClient(request);

        // Phase 2: Validate
        ValidateInvoiceExists(invoice, request);
        ValidateInvoiceIsDraft(invoice!, request);

        // Phase 3: Execute
        var response = await PerformRecalculation(invoice!, client!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? Invoice, Client? Client)> GetInvoiceAndClient(
        RecalculateInvoiceRequest request)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId);
        Client? client = null;

        if (invoice != null)
        {
            client = await _clientRepository.GetByIdAsync(invoice.ClientId);
        }

        return (invoice, client);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? invoice, RecalculateInvoiceRequest request)
    {
        if (invoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested recalculation of non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateInvoiceIsDraft(Invoice invoice, RecalculateInvoiceRequest request)
    {
        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to recalculate non-draft invoice {InvoiceId} (status: {Status})",
                request.Actor.UserId, request.InvoiceId, invoice.Status);

            throw new ForbiddenOperationException(
                "RecalculateInvoice",
                "Fattura",
                $"Solo le fatture in bozza possono essere ricalcolate. Stato attuale: {invoice.Status}.");
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<RecalculateInvoiceResponse> PerformRecalculation(
        Invoice invoice, Client client, RecalculateInvoiceRequest request)
    {
        // Attach client for ritenuta/split-payment calculation
        invoice.Client = client;

        // Recalculate all totals
        _calculationService.CalculateInvoiceTotals(invoice);

        // Persist
        var updated = await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} recalculated successfully by actor {ActorId}. TotalDue: {TotalDue}",
            invoice.Id, request.Actor.UserId, invoice.TotalDue);

        return new RecalculateInvoiceResponse(updated!);
    }
}
