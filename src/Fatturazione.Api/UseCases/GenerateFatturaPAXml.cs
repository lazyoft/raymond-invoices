using Fatturazione.Domain.Exceptions;
using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;

namespace Fatturazione.Api.UseCases;

// Request: actor asks to generate FatturaPA XML for a specific invoice
public record GenerateFatturaPAXmlRequest(Guid InvoiceId, ActorContext Actor);

// Response: the generated FatturaPA XML string
public record GenerateFatturaPAXmlResponse(string Xml);

/// <summary>
/// Generates FatturaPA-compliant XML (tracciato v1.2.2) for electronic invoicing.
/// Loads the invoice with its client and issuer profile, validates all entities
/// exist, then generates the XML using the FatturaPA schema.
/// DL 119/2018, DL 127/2015, Provvedimento AdE 89757/2018.
/// </summary>
/// <remarks>
/// Placed in the Api project (rather than Domain) because the repository interfaces currently
/// reside in Fatturazione.Infrastructure.Repositories, and Domain cannot reference Infrastructure
/// without introducing a circular dependency. When repository interfaces are migrated to Domain,
/// this class should move to Fatturazione.Domain.UseCases.
/// </remarks>
public class GenerateFatturaPAXml : IUseCase<GenerateFatturaPAXmlRequest, GenerateFatturaPAXmlResponse>
{
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IIssuerProfileRepository _issuerProfileRepository;
    private readonly IFatturaPAXmlService _xmlService;
    private readonly ILogger<GenerateFatturaPAXml> _logger;

    public GenerateFatturaPAXml(
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IIssuerProfileRepository issuerProfileRepository,
        IFatturaPAXmlService xmlService,
        ILogger<GenerateFatturaPAXml> logger)
    {
        _invoiceRepository = invoiceRepository;
        _clientRepository = clientRepository;
        _issuerProfileRepository = issuerProfileRepository;
        _xmlService = xmlService;
        _logger = logger;
    }

    // -- Sole public method --------------------------------------------------
    public async Task<GenerateFatturaPAXmlResponse> Execute(GenerateFatturaPAXmlRequest request)
    {
        _logger.LogInformation(
            "Actor {ActorId} requested FatturaPA XML generation for invoice {InvoiceId}",
            request.Actor.UserId, request.InvoiceId);

        // Phase 1: Load
        var (invoice, client, issuer) = await GetInvoiceWithDependencies(request);

        // Phase 2: Validate
        ValidateInvoiceExists(invoice, request);
        ValidateClientExists(client, invoice!, request);
        ValidateIssuerProfileExists(issuer, request);

        // Phase 3: Execute
        var response = PerformXmlGeneration(invoice!, client!, issuer!, request);

        return response;
    }

    // -- Phase 1: Load -------------------------------------------------------

    private async Task<(Invoice? Invoice, Client? Client, IssuerProfile? Issuer)>
        GetInvoiceWithDependencies(GenerateFatturaPAXmlRequest request)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId);

        Client? client = null;
        if (invoice != null)
        {
            client = await _clientRepository.GetByIdAsync(invoice.ClientId);
        }

        var issuer = await _issuerProfileRepository.GetAsync();

        return (invoice, client, issuer);
    }

    // -- Phase 2: Validate ---------------------------------------------------

    private void ValidateInvoiceExists(Invoice? invoice, GenerateFatturaPAXmlRequest request)
    {
        if (invoice == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested XML for non-existent invoice {InvoiceId}",
                request.Actor.UserId, request.InvoiceId);

            throw new NotFoundException("Fattura", request.InvoiceId);
        }
    }

    private void ValidateClientExists(Client? client, Invoice invoice, GenerateFatturaPAXmlRequest request)
    {
        if (client == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested XML for invoice {InvoiceId} but client {ClientId} not found",
                request.Actor.UserId, request.InvoiceId, invoice.ClientId);

            throw new NotFoundException("Cliente", invoice.ClientId);
        }
    }

    private void ValidateIssuerProfileExists(IssuerProfile? issuer, GenerateFatturaPAXmlRequest request)
    {
        if (issuer == null)
        {
            _logger.LogInformation(
                "Actor {ActorId} requested XML for invoice {InvoiceId} but issuer profile is not configured",
                request.Actor.UserId, request.InvoiceId);

            throw new InvalidInputException(
                "Profilo emittente non configurato. Configurare tramite PUT /api/issuer-profile");
        }
    }

    // -- Phase 3: Execute ----------------------------------------------------

    private GenerateFatturaPAXmlResponse PerformXmlGeneration(
        Invoice invoice, Client client, IssuerProfile issuer, GenerateFatturaPAXmlRequest request)
    {
        // Attach client navigation property for XML generation
        invoice.Client = client;

        // Generate FatturaPA XML
        var xml = _xmlService.GenerateXml(invoice, issuer);

        _logger.LogInformation(
            "FatturaPA XML generated for invoice {InvoiceId} ({InvoiceNumber}) by actor {ActorId}",
            invoice.Id, invoice.InvoiceNumber, request.Actor.UserId);

        return new GenerateFatturaPAXmlResponse(xml);
    }
}
