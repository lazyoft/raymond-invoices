using Fatturazione.Domain.Models;
using Fatturazione.Api.UseCases;
using Fatturazione.Domain.UseCases;
using Fatturazione.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fatturazione.Api.Endpoints;

/// <summary>
/// DTO for invoice status transition requests
/// </summary>
public record TransitionRequest(InvoiceStatus NewStatus);

/// <summary>
/// DTO for credit note creation requests
/// </summary>
public record CreditNoteRequest(string Reason);

/// <summary>
/// DTO for debit note creation requests
/// </summary>
public record DebitNoteRequest(List<InvoiceItem> Items, string Reason);

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

        group.MapGet("/{id:guid}/xml", GetInvoiceXml)
            .WithName("GetInvoiceXml")
            .WithDescription("Generate FatturaPA XML for an invoice (DL 119/2018)")
            .Produces<string>(200, "application/xml")
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapPost("/{id:guid}/credit-note", CreateCreditNote)
            .WithName("CreateCreditNote")
            .WithDescription("Create a credit note (TD04) for an invoice (Art. 26 DPR 633/72)")
            .Produces<Invoice>(201)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapPost("/{id:guid}/debit-note", CreateDebitNote)
            .WithName("CreateDebitNote")
            .WithDescription("Create a debit note (TD05) for an invoice (Art. 26 DPR 633/72)")
            .Produces<Invoice>(201)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

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
        UseCases.CreateInvoice useCase)
    {
        var request = new CreateInvoiceRequest(invoice, ActorContext.System);
        var response = await useCase.Execute(request);
        return Results.Created($"/api/invoices/{response.Invoice.Id}", response.Invoice);
    }

    private static async Task<IResult> UpdateInvoice(
        Guid id,
        Invoice invoice,
        UseCases.UpdateInvoice useCase)
    {
        // HTTP-level guard: route param must match body ID
        if (id != invoice.Id)
        {
            return Results.BadRequest("ID mismatch");
        }

        try
        {
            var request = new UpdateInvoiceRequest(id, invoice, ActorContext.System);
            var response = await useCase.Execute(request);
            return Results.Ok(response.Invoice);
        }
        catch (Fatturazione.Domain.Exceptions.ForbiddenOperationException ex)
        {
            // Map to 400 (ValidationProblem) for backward compatibility:
            // issued-invoice immutability is a validation concern from the client's perspective
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { ex.Reason ?? ex.Message } }
            });
        }
    }

    private static async Task<IResult> CalculateInvoiceTotals(
        Guid id,
        RecalculateInvoice useCase)
    {
        var request = new RecalculateInvoiceRequest(id, ActorContext.System);
        var response = await useCase.Execute(request);
        return Results.Ok(response.Invoice);
    }

    private static async Task<IResult> IssueInvoice(
        Guid id,
        UseCases.IssueInvoice useCase)
    {
        try
        {
            var request = new IssueInvoiceRequest(id, ActorContext.System);
            var response = await useCase.Execute(request);
            return Results.Ok(response.Invoice);
        }
        catch (Fatturazione.Domain.Exceptions.ForbiddenOperationException ex)
        {
            // Map to 400 (ValidationProblem) for backward compatibility:
            // invalid status transitions are a validation concern from the client's perspective
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { ex.Reason ?? ex.Message } }
            });
        }
    }

    private static async Task<IResult> TransitionInvoice(
        Guid id,
        TransitionRequest request,
        UseCases.TransitionInvoiceStatus useCase)
    {
        try
        {
            var useCaseRequest = new TransitionInvoiceStatusRequest(id, request.NewStatus, ActorContext.System);
            var response = await useCase.Execute(useCaseRequest);
            return Results.Ok(new TransitionResponse(response.Invoice, response.CreditNoteWarning));
        }
        catch (Fatturazione.Domain.Exceptions.NotFoundException)
        {
            return Results.NotFound();
        }
        catch (Fatturazione.Domain.Exceptions.ForbiddenOperationException ex)
        {
            // Map to 400 (ValidationProblem) for backward compatibility:
            // invalid status transitions are a validation concern from the client's perspective
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { ex.Reason ?? ex.Message } }
            });
        }
    }

    private static async Task<IResult> DeleteInvoice(
        Guid id,
        UseCases.DeleteInvoice useCase)
    {
        var request = new DeleteInvoiceRequest(id, ActorContext.System);
        await useCase.Execute(request);
        return Results.NoContent();
    }

    private static async Task<IResult> GetInvoiceXml(
        Guid id,
        UseCases.GenerateFatturaPAXml useCase)
    {
        try
        {
            var request = new GenerateFatturaPAXmlRequest(id, ActorContext.System);
            var response = await useCase.Execute(request);
            return Results.Content(response.Xml, "application/xml");
        }
        catch (Fatturazione.Domain.Exceptions.NotFoundException)
        {
            return Results.NotFound();
        }
        catch (Fatturazione.Domain.Exceptions.InvalidInputException ex)
        {
            var errorDict = ex.Errors
                .Select((e, i) => new { Key = i == 0 ? "IssuerProfile" : $"Error{i}", Value = e })
                .ToDictionary(x => x.Key, x => new[] { x.Value });
            return Results.ValidationProblem(errorDict);
        }
        catch (Fatturazione.Domain.Exceptions.ForbiddenOperationException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "Status", new[] { ex.Reason ?? ex.Message } }
            });
        }
    }

    private static async Task<IResult> CreateCreditNote(
        Guid id,
        CreditNoteRequest request,
        UseCases.CreateCreditNote useCase)
    {
        var useCaseRequest = new CreateCreditNoteRequest(id, request.Reason, ActorContext.System);
        var response = await useCase.Execute(useCaseRequest);
        return Results.Created($"/api/invoices/{response.CreditNote.Id}", response.CreditNote);
    }

    private static async Task<IResult> CreateDebitNote(
        Guid id,
        DebitNoteRequest request,
        UseCases.CreateDebitNote useCase)
    {
        var useCaseRequest = new CreateDebitNoteRequest(id, request.Items, request.Reason, ActorContext.System);
        var response = await useCase.Execute(useCaseRequest);
        return Results.Created($"/api/invoices/{response.DebitNote.Id}", response.DebitNote);
    }
}
