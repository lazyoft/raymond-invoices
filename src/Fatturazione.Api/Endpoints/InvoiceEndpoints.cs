using Fatturazione.Domain.Models;
using Fatturazione.Domain.Services;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fatturazione.Api.Endpoints;

/// <summary>
/// DTO for invoice status transition requests
/// </summary>
public record TransitionRequest(InvoiceStatus NewStatus);

/// <summary>
/// Response for invoice status transition, including optional warnings
/// </summary>
public record TransitionResponse(Invoice Invoice, string? Warning);

/// <summary>
/// Endpoints for managing invoices
/// </summary>
public static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllInvoices)
            .WithName("GetAllInvoices")
            .WithDescription("Get all invoices")
            .Produces<IEnumerable<Invoice>>(200);

        group.MapGet("/{id:guid}", GetInvoiceById)
            .WithName("GetInvoiceById")
            .WithDescription("Get invoice by ID")
            .Produces<Invoice>(200)
            .Produces(404);

        group.MapGet("/by-client/{clientId:guid}", GetInvoicesByClient)
            .WithName("GetInvoicesByClient")
            .WithDescription("Get all invoices for a specific client")
            .Produces<IEnumerable<Invoice>>(200);

        group.MapPost("/", CreateInvoice)
            .WithName("CreateInvoice")
            .WithDescription("Create a new invoice")
            .Produces<Invoice>(201)
            .Produces<ValidationProblemDetails>(400);

        group.MapPut("/{id:guid}", UpdateInvoice)
            .WithName("UpdateInvoice")
            .WithDescription("Update an existing invoice")
            .Produces<Invoice>(200)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapPost("/{id:guid}/calculate", CalculateInvoiceTotals)
            .WithName("CalculateInvoiceTotals")
            .WithDescription("Recalculate invoice totals")
            .Produces<Invoice>(200)
            .Produces(404);

        group.MapPost("/{id:guid}/issue", IssueInvoice)
            .WithName("IssueInvoice")
            .WithDescription("Issue an invoice (change status to Issued and assign number)")
            .Produces<Invoice>(200)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapPost("/{id:guid}/transition", TransitionInvoice)
            .WithName("TransitionInvoice")
            .WithDescription("Transition an invoice to a new status")
            .Produces<TransitionResponse>(200)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapDelete("/{id:guid}", DeleteInvoice)
            .WithName("DeleteInvoice")
            .WithDescription("Delete an invoice")
            .Produces(204)
            .Produces(404);

        return group;
    }

    private static async Task<IResult> GetAllInvoices(IInvoiceRepository repository)
    {
        var invoices = await repository.GetAllAsync();
        return Results.Ok(invoices);
    }

    private static async Task<IResult> GetInvoiceById(Guid id, IInvoiceRepository repository)
    {
        var invoice = await repository.GetByIdAsync(id);
        return invoice != null ? Results.Ok(invoice) : Results.NotFound();
    }

    private static async Task<IResult> GetInvoicesByClient(
        Guid clientId,
        IInvoiceRepository repository)
    {
        var invoices = await repository.GetByClientIdAsync(clientId);
        return Results.Ok(invoices);
    }

    private static async Task<IResult> CreateInvoice(
        Invoice invoice,
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService)
    {
        // Validate client exists
        var clientExists = await clientRepository.ExistsAsync(invoice.ClientId);
        if (!clientExists)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "ClientId", new[] { "Client non trovato" } }
            });
        }

        // Load client for calculations
        invoice.Client = await clientRepository.GetByIdAsync(invoice.ClientId);

        // Validate invoice
        var (isValid, errors) = InvoiceValidator.Validate(invoice);
        if (!isValid)
        {
            var errorDict = errors.Select((e, i) => new { Key = $"Error{i}", Value = e })
                .ToDictionary(x => x.Key, x => new[] { x.Value });
            return Results.ValidationProblem(errorDict);
        }

        // Calculate totals
        calculationService.CalculateInvoiceTotals(invoice);

        // Create invoice
        var created = await invoiceRepository.CreateAsync(invoice);
        return Results.Created($"/api/invoices/{created.Id}", created);
    }

    private static async Task<IResult> UpdateInvoice(
        Guid id,
        Invoice invoice,
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService)
    {
        if (id != invoice.Id)
        {
            return Results.BadRequest("ID mismatch");
        }

        var existing = await invoiceRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return Results.NotFound();
        }

        // Check immutability: issued invoices cannot be modified (Art. 21 DPR 633/72)
        if (existing.Status != InvoiceStatus.Draft)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { "Cannot modify an issued invoice. Use credit/debit note (nota di credito/debito) instead." } }
            });
        }

        // Validate client exists
        var clientExists = await clientRepository.ExistsAsync(invoice.ClientId);
        if (!clientExists)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "ClientId", new[] { "Client non trovato" } }
            });
        }

        // Load client for calculations
        invoice.Client = await clientRepository.GetByIdAsync(invoice.ClientId);

        // Validate invoice
        var (isValid, errors) = InvoiceValidator.Validate(invoice);
        if (!isValid)
        {
            var errorDict = errors.Select((e, i) => new { Key = $"Error{i}", Value = e })
                .ToDictionary(x => x.Key, x => new[] { x.Value });
            return Results.ValidationProblem(errorDict);
        }

        // Recalculate totals
        calculationService.CalculateInvoiceTotals(invoice);

        // Update invoice
        var updated = await invoiceRepository.UpdateAsync(invoice);
        return Results.Ok(updated);
    }

    private static async Task<IResult> CalculateInvoiceTotals(
        Guid id,
        IInvoiceRepository invoiceRepository,
        IClientRepository clientRepository,
        IInvoiceCalculationService calculationService)
    {
        var invoice = await invoiceRepository.GetByIdAsync(id);
        if (invoice == null)
        {
            return Results.NotFound();
        }

        // Load client for ritenuta calculation
        invoice.Client = await clientRepository.GetByIdAsync(invoice.ClientId);

        // Recalculate
        calculationService.CalculateInvoiceTotals(invoice);

        // Update
        var updated = await invoiceRepository.UpdateAsync(invoice);
        return Results.Ok(updated);
    }

    private static async Task<IResult> IssueInvoice(
        Guid id,
        IInvoiceRepository repository,
        IClientRepository clientRepository,
        IInvoiceNumberingService numberingService,
        IInvoiceCalculationService calculationService)
    {
        var invoice = await repository.GetByIdAsync(id);
        if (invoice == null)
        {
            return Results.NotFound();
        }

        // Check if can transition to Issued status
        if (!InvoiceValidator.CanTransitionTo(invoice.Status, InvoiceStatus.Issued))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { $"Cannot transition from {invoice.Status} to Issued" } }
            });
        }

        // Load client for calculations
        invoice.Client = await clientRepository.GetByIdAsync(invoice.ClientId);

        // Recalculate totals before issuing (Bug #8 fix)
        calculationService.CalculateInvoiceTotals(invoice);

        // Generate invoice number
        var lastInvoiceNumber = await repository.GetLastInvoiceNumberAsync();
        invoice.InvoiceNumber = numberingService.GenerateNextInvoiceNumber(lastInvoiceNumber);

        // Update status
        invoice.Status = InvoiceStatus.Issued;

        // Update invoice
        var updated = await repository.UpdateAsync(invoice);
        return Results.Ok(updated);
    }

    private static async Task<IResult> TransitionInvoice(
        Guid id,
        TransitionRequest request,
        IInvoiceRepository repository)
    {
        var invoice = await repository.GetByIdAsync(id);
        if (invoice == null)
        {
            return Results.NotFound();
        }

        // Validate the transition using existing domain logic
        if (!InvoiceValidator.CanTransitionTo(invoice.Status, request.NewStatus))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { $"Cannot transition from {invoice.Status} to {request.NewStatus}" } }
            });
        }

        // Determine if a credit note warning is needed (Art. 26 DPR 633/72)
        // Cancelling a non-Draft invoice requires a credit note (nota di credito)
        string? warning = null;
        if (request.NewStatus == InvoiceStatus.Cancelled && invoice.Status != InvoiceStatus.Draft)
        {
            warning = "L'annullamento di una fattura già emessa richiede l'emissione di una nota di credito (Art. 26 DPR 633/72).";
        }

        // Update the status
        invoice.Status = request.NewStatus;

        // Save
        var updated = await repository.UpdateAsync(invoice);

        return Results.Ok(new TransitionResponse(updated!, warning));
    }

    private static async Task<IResult> DeleteInvoice(Guid id, IInvoiceRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }
}
