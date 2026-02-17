using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor asks to delete an invoice by ID
public record DeleteInvoiceRequest(Guid InvoiceId, ActorContext Actor);

// Response: confirmation that the invoice was deleted
public record DeleteInvoiceResponse(bool Success);

/// <summary>
/// Deletes a draft invoice: validates the invoice exists and is still in Draft status,
/// then removes it from persistence. Issued invoices cannot be deleted -- they must
/// be cancelled via credit note (Art. 26 DPR 633/72).
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class DeleteInvoice : IUseCase<DeleteInvoiceRequest, DeleteInvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<DeleteInvoice> _logger;

    public DeleteInvoice(
        IInvoiceRepository invoiceRepository,
        ILogger<DeleteInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<DeleteInvoiceResponse> Execute(DeleteInvoiceRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested deletion of invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var invoice = await GetInvoice(request);

        // Phase 2: Validate
        ValidateInvoiceExists(invoice, request);
        ValidateInvoiceIsDraft(invoice!, request);

        // Phase 3: Execute
        var response = await PerformDeletion(invoice!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<Invoice?> GetInvoice(DeleteInvoiceRequest request)
    {
        return await _invoiceRepository.GetByIdAsync(request.InvoiceId);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? invoice, DeleteInvoiceRequest request)
    {
        if (invoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to delete non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateInvoiceIsDraft(Invoice invoice, DeleteInvoiceRequest request)
    {
        if (invoice.Status != InvoiceStatus.Draft)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to delete non-draft invoice {InvoiceId} (status: {Status})",
                request.Actor.UserId, request.InvoiceId, invoice.Status);

            throw new ForbiddenOperationException(
                "DeleteInvoice",
                "Fattura",
                "Solo le fatture in bozza possono essere eliminate. Le fatture emesse devono essere annullate tramite nota di credito (Art. 26 DPR 633/72).");
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<DeleteInvoiceResponse> PerformDeletion(Invoice invoice, DeleteInvoiceRequest request)
    {
        await _invoiceRepository.DeleteAsync(invoice.Id);

        _logger.LogInformation(
            "Invoice {InvoiceId} deleted successfully by actor {ActorId}",
            invoice.Id, request.Actor.UserId);

        return new DeleteInvoiceResponse(true);
    }
}
