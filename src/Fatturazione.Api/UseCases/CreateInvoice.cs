using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor provides invoice data to create a new invoice
public record CreateInvoiceRequest(Invoice Invoice, ActorContext Actor);

// Response: the created invoice with calculated totals
public record CreateInvoiceResponse(Invoice Invoice);

/// <summary>
/// Creates a new invoice: validates client and invoice data, calculates totals, and persists.
/// Only invoices with a valid existing client can be created.
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class CreateInvoice : IUseCase<CreateInvoiceRequest, CreateInvoiceResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IInvoiceCalculationService _calculationService;
    private readonly ILogger<CreateInvoice> _logger;

    public CreateInvoice(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService,
        ILogger<CreateInvoice> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _calculationService = calculationService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<CreateInvoiceResponse> Execute(CreateInvoiceRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested creation of invoice for client {ClientId}",
            request.Actor.UserId, request.Invoice.ClientId);

        // Phase 1: Load
        var client = await GetClient(request);

        // Phase 2: Validate
        ValidateClientExists(client, request);
        ValidateInvoiceData(request.Invoice, request);

        // Phase 3: Execute
        var response = await PerformCreation(request.Invoice, client!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<Client?> GetClient(CreateInvoiceRequest request)
    {
        var client = await _clientRepository.GetByIdAsync(request.Invoice.ClientId);
        return client;
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateClientExists(Client? client, CreateInvoiceRequest request)
    {
        if (client == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} attempted to create invoice with non-existent client {ClientId}",
                request.Actor.UserId, request.Invoice.ClientId);

            throw new InvalidInputException("Client non trovato");
        }
    }

    private void ValidateInvoiceData(Invoice invoice, CreateInvoiceRequest request)
    {
        var (isValid, errors) = InvoiceValidator.Validate(invoice);
        if (!isValid)
        {
            _logger.LogInformation(
                "Actor {ActorId} submitted invalid invoice data: {Errors}",
                request.Actor.UserId, string.Join("; ", errors));

            throw new InvalidInputException(errors);
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private async Task<CreateInvoiceResponse> PerformCreation(
        Invoice invoice, Client client, CreateInvoiceRequest request)
    {
        // Attach client for calculation (ritenuta, split payment, etc.)
        invoice.Client = client;

        // Calculate all totals
        _calculationService.CalculateInvoiceTotals(invoice);

        // Persist
        var created = await _invoiceRepository.CreateAsync(invoice);

        _logger.LogInformation(
            "Invoice {InvoiceId} created successfully by actor {ActorId} for client {ClientId}. TotalDue: {TotalDue}",
            created.Id, request.Actor.UserId, invoice.ClientId, invoice.TotalDue);

        return new CreateInvoiceResponse(created);
    }
}
