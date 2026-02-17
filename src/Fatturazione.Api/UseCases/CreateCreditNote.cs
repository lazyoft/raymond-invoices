using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor provides the original invoice ID and reason for the credit note
public record CreateCreditNoteRequest(Guid InvoiceId, string Reason, ActorContext Actor);

// Response: the created credit note document
public record CreateCreditNoteResponse(Invoice CreditNote);

/// <summary>
/// Creates a credit note (TD04) linked to an existing invoice.
/// Loads the original invoice, validates it can receive a credit note,
/// creates the credit note document, calculates totals, and persists.
/// Art. 26 DPR 633/72 - Variazioni in diminuzione.
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class CreateCreditNote : IUseCase<CreateCreditNoteRequest, CreateCreditNoteResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICreditNoteService _creditNoteService;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<CreateCreditNote> _logger;

    public CreateCreditNote(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        ICreditNoteService creditNoteService,
        IInvoiceCalculationService calculationService,
        ILogger<CreateCreditNote> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _creditNoteService = creditNoteService;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<CreateCreditNoteResponse> Execute(CreateCreditNoteRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested creation of credit note for invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (originalInvoice, client) = await GetOriginalInvoiceWithClient(request);

        // Phase 2: Validate
        ValidateOriginalInvoiceExists(originalInvoice, request);
        var creditNote = CreateAndValidateCreditNote(originalInvoice!, request);

        // Phase 3: Execute
        var response = await PerformCreation(creditNote, client, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? OriginalInvoice, Client? Client)>
        GetOriginalInvoiceWithClient(CreateCreditNoteRequest request)
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

    private void ValidateOriginalInvoiceExists(Invoice? originalInvoice, CreateCreditNoteRequest request)
    {
        if (originalInvoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested credit note for non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private Invoice CreateAndValidateCreditNote(Invoice originalInvoice, CreateCreditNoteRequest request)
    {
        // Create the credit note document via the domain service
        var creditNote = _creditNoteService.CreateCreditNote(originalInvoice, request.Reason);

        // Validate using the domain service (checks status, amounts, references)
        var (isValid, errors) = _creditNoteService.ValidateCreditNote(creditNote, originalInvoice);
        if (!isValid)
        {
            _logger.LogInformation(
                "Actor {ActorId} credit note validation failed for invoice {InvoiceId}: {Errors}",
                request.Actor.UserId, request.InvoiceId, string.Join("; ", errors));

            throw new InvalidInputException(errors);
        }

        return creditNote;
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<CreateCreditNoteResponse> PerformCreation(
        Invoice creditNote, Client? client, CreateCreditNoteRequest request)
    {
        // Attach client for calculation (ritenuta, split payment, etc.)
        creditNote.Client = client;

        // Calculate totals
        _calculationService.CalculateInvoiceTotals(creditNote);

        // Persist
        var created = await _invoiceRepository.CreateAsync(creditNote);

        _logger.LogInformation(
            "Credit note {CreditNoteId} (TD04) created for invoice {InvoiceId} by actor {ActorId}. TotalDue: {TotalDue}",
            created.Id, request.InvoiceId, request.Actor.UserId, created.TotalDue);

        return new CreateCreditNoteResponse(created);
    }
}
