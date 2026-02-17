using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor provides the updated invoice data
public record UpdateInvoiceRequest(Guid InvoiceId, Invoice Invoice, ActorContext Actor);

// Response: the persisted invoice with recalculated totals
public record UpdateInvoiceResponse(Invoice Invoice);

/// <summary>
/// Updates an existing draft invoice: validates the invoice is still editable,
/// the client exists, the data is valid, recalculates totals, and persists.
/// Issued invoices cannot be modified (Art. 21 DPR 633/72).
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class UpdateInvoice : IUseCase<UpdateInvoiceRequest, UpdateInvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<UpdateInvoice> _logger;

    public UpdateInvoice(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService,
        ILogger<UpdateInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<UpdateInvoiceResponse> Execute(UpdateInvoiceRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested update of invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (existing, client) = await GetInvoiceAndClient(request);

        // Phase 2: Validate
        ValidateInvoiceExists(existing, request);
        ValidateInvoiceIsDraft(existing!, request);
        ValidateClientExists(client, request);
        ValidateInvoiceData(request.Invoice, request);

        // Phase 3: Execute
        var response = await PerformUpdate(request.Invoice, client!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? Existing, Client? Client)> GetInvoiceAndClient(
        UpdateInvoiceRequest request)
    {
        var existing = await _invoiceRepository.GetByIdAsync(request.InvoiceId);

        Client? client = null;
        if (existing != null)
        {
            client = await _clientRepository.GetByIdAsync(request.Invoice.ClientId);
        }

        return (existing, client);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? existing, UpdateInvoiceRequest request)
    {
        if (existing == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to update non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateInvoiceIsDraft(Invoice existing, UpdateInvoiceRequest request)
    {
        if (existing.Status != InvoiceStatus.Draft)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to modify non-draft invoice {InvoiceId} (status: {Status})",
                request.Actor.UserId, request.InvoiceId, existing.Status);

            throw new ForbiddenOperationException(
                "UpdateInvoice",
                "Fattura",
                "Cannot modify an issued invoice. Use credit/debit note (nota di credito/debito) instead.");
        }
    }

    private void ValidateClientExists(Client? client, UpdateInvoiceRequest request)
    {
        if (client == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to update invoice {InvoiceId} with non-existent client {ClientId}",
                request.Actor.UserId, request.InvoiceId, request.Invoice.ClientId);

            throw new InvalidInputException("Client non trovato");
        }
    }

    private void ValidateInvoiceData(Invoice invoice, UpdateInvoiceRequest request)
    {
        var (isValid, errors) = InvoiceValidator.Validate(invoice);
        if (!isValid)
        {
            _logger.LogInformation(
                "Actor {ActorId} submitted invalid invoice data for {InvoiceId}: {Errors}",
                request.Actor.UserId, request.InvoiceId, string.Join("; ", errors));

            throw new InvalidInputException(errors);
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<UpdateInvoiceResponse> PerformUpdate(
        Invoice invoice, Client client, UpdateInvoiceRequest request)
    {
        // Attach client for calculation (ritenuta, split payment, etc.)
        invoice.Client = client;

        // Recalculate all totals
        _calculationService.CalculateInvoiceTotals(invoice);

        // Persist
        var updated = await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} updated successfully by actor {ActorId}. TotalDue: {TotalDue}",
            invoice.Id, request.Actor.UserId, invoice.TotalDue);

        return new UpdateInvoiceResponse(updated!);
    }
}
