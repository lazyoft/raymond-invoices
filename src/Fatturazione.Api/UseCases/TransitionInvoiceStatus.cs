using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.UseCases;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor asks to transition an invoice to a new status
public record TransitionInvoiceStatusRequest(Guid InvoiceId, InvoiceStatus NewStatus, ActorContext Actor);

// Response: the updated invoice plus an optional credit note warning (Art. 26 DPR 633/72)
public record TransitionInvoiceStatusResponse(Invoice Invoice, string? CreditNoteWarning);

/// <summary>
/// Transitions an invoice to a new status: validates the transition is allowed
/// by the state machine, applies the status change, and returns a warning when
/// cancelling a non-draft invoice (credit note required per Art. 26 DPR 633/72).
/// </summary>
/// <remarks>
/// This use case handles generic status transitions (Issued->Sent, Sent->Overdue, etc.).
/// The Draft->Issued transition should preferably go through the IssueInvoice use case,
/// which also assigns the progressive invoice number. However, this endpoint does NOT
/// block Draft->Issued since the validator allows it -- it simply won't assign a number.
///
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class TransitionInvoiceStatus : IUseCase<TransitionInvoiceStatusRequest, TransitionInvoiceStatusResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ILogger<TransitionInvoiceStatus> _logger;

    public TransitionInvoiceStatus(
        IInvoiceRepository invoiceRepository,
        ILogger<TransitionInvoiceStatus> logger)
    {
        _invoiceRepository = invoiceRepository;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<TransitionInvoiceStatusResponse> Execute(TransitionInvoiceStatusRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested transition of invoice {InvoiceId} to {NewStatus}",
            request.Actor.UserId, request.InvoiceId, request.NewStatus);

        // Phase 1: Load
        var invoice = await GetInvoice(request);

        // Phase 2: Validate
        ValidateInvoiceExists(invoice, request);
        ValidateTransitionAllowed(invoice!, request);

        // Phase 3: Execute
        var response = await PerformTransition(invoice!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<Invoice?> GetInvoice(TransitionInvoiceStatusRequest request)
    {
        return await _invoiceRepository.GetByIdAsync(request.InvoiceId);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? invoice, TransitionInvoiceStatusRequest request)
    {
        if (invoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested transition of non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateTransitionAllowed(Invoice invoice, TransitionInvoiceStatusRequest request)
    {
        if (!InvoiceValidator.CanTransitionTo(invoice.Status, request.NewStatus))
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted forbidden transition {From} -> {To} on invoice {InvoiceId}",
                request.Actor.UserId, invoice.Status, request.NewStatus, request.InvoiceId);

            throw new ForbiddenOperationException(
                "TransitionInvoiceStatus",
                "Fattura",
                $"Cannot transition from {invoice.Status} to {request.NewStatus}");
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<TransitionInvoiceStatusResponse> PerformTransition(
        Invoice invoice, TransitionInvoiceStatusRequest request)
    {
        // Determine if a credit note warning is needed (Art. 26 DPR 633/72)
        // Cancelling a non-Draft invoice requires a credit note (nota di credito)
        string? creditNoteWarning = null;
        if (request.NewStatus == InvoiceStatus.Cancelled && invoice.Status != InvoiceStatus.Draft)
        {
            creditNoteWarning = "L'annullamento di una fattura già emessa richiede l'emissione di una nota di credito (Art. 26 DPR 633/72).";
        }

        // Apply the status transition
        invoice.Status = request.NewStatus;

        // Persist
        var updated = await _invoiceRepository.UpdateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} transitioned to {NewStatus} by actor {ActorId}",
            invoice.Id, request.NewStatus, request.Actor.UserId);

        return new TransitionInvoiceStatusResponse(updated!, creditNoteWarning);
    }
}
