using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor provides the original invoice ID, additional items, and reason for the debit note
public record CreateDebitNoteRequest(Guid InvoiceId, List<InvoiceItem> Items, string Reason, ActorContext Actor);

// Response: the created debit note document
public record CreateDebitNoteResponse(Invoice DebitNote);

/// <summary>
/// Creates a debit note (TD05) linked to an existing invoice.
/// Loads the original invoice, validates it can receive a debit note,
/// creates the debit note document with additional items, calculates totals, and persists.
/// Art. 26, comma 1, DPR 633/72 - Variazioni in aumento.
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class CreateDebitNote : IUseCase<CreateDebitNoteRequest, CreateDebitNoteResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICreditNoteService _creditNoteService;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<CreateDebitNote> _logger;

    public CreateDebitNote(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        ICreditNoteService creditNoteService,
        IInvoiceCalculationService calculationService,
        ILogger<CreateDebitNote> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _creditNoteService = creditNoteService;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<CreateDebitNoteResponse> Execute(CreateDebitNoteRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested creation of debit note for invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (originalInvoice, client) = await GetOriginalInvoiceWithClient(request);

        // Phase 2: Validate
        ValidateOriginalInvoiceExists(originalInvoice, request);
        var debitNote = CreateAndValidateDebitNote(originalInvoice!, request);

        // Phase 3: Execute
        var response = await PerformCreation(debitNote, client, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? OriginalInvoice, Client? Client)>
        GetOriginalInvoiceWithClient(CreateDebitNoteRequest request)
    {
        var originalInvoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId);

        Client? client = null;
        if (originalInvoice != null)
        {
            client = await _clientRepository.GetByIdAsync(originalInvoice.ClientId);
            originalInvoice.Client = client;
        }

        return (originalInvoice, client);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateOriginalInvoiceExists(Invoice? originalInvoice, CreateDebitNoteRequest request)
    {
        if (originalInvoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested debit note for non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private Invoice CreateAndValidateDebitNote(Invoice originalInvoice, CreateDebitNoteRequest request)
    {
        // Create the debit note document via the domain service
        var debitNote = _creditNoteService.CreateDebitNote(originalInvoice, request.Items, request.Reason);

        // Validate using the domain service (checks status, references, document type)
        var (isValid, errors) = _creditNoteService.ValidateCreditNote(debitNote, originalInvoice);
        if (!isValid)
        {
            _logger.LogInformation(
                "Actor {ActorId} debit note validation failed for invoice {InvoiceId}: {Errors}",
                request.Actor.UserId, request.InvoiceId, string.Join("; ", errors));

            throw new InvalidInputException(errors);
        }

        return debitNote;
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<CreateDebitNoteResponse> PerformCreation(
        Invoice debitNote, Client? client, CreateDebitNoteRequest request)
    {
        // Attach client for calculation (ritenuta, split payment, etc.)
        debitNote.Client = client;

        // Calculate totals
        _calculationService.CalculateInvoiceTotals(debitNote);

        // Persist
        var created = await _invoiceRepository.CreateAsync(debitNote);

        _logger.LogInformation(
            "Debit note {DebitNoteId} (TD05) created for invoice {InvoiceId} by actor {ActorId}. TotalDue: {TotalDue}",
            created.Id, request.InvoiceId, request.Actor.UserId, created.TotalDue);

        return new CreateDebitNoteResponse(created);
    }
}
