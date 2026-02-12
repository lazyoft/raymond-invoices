using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fatturazione.Api.Endpoints;

/// <summary>
/// Endpoints for managing clients
/// </summary>
public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllClients)
            .WithName("GetAllClients")
            .WithDescription("Get all clients")
            .Produces<IEnumerable<Client>>(200);

        group.MapGet("/{id:guid}", GetClientById)
            .WithName("GetClientById")
            .WithDescription("Get client by ID")
            .Produces<Client>(200)
            .Produces(404);

        group.MapPost("/", CreateClient)
            .WithName("CreateClient")
            .WithDescription("Create a new client")
            .Produces<Client>(201)
            .Produces<ValidationProblemDetails>(400);

        group.MapPut("/{id:guid}", UpdateClient)
            .WithName("UpdateClient")
            .WithDescription("Update an existing client")
            .Produces<Client>(200)
            .Produces(404)
            .Produces<ValidationProblemDetails>(400);

        group.MapDelete("/{id:guid}", DeleteClient)
            .WithName("DeleteClient")
            .WithDescription("Delete a client")
            .Produces(204)
            .Produces(404);

        group.MapGet("/validate-partita-iva/{partitaIva}", ValidatePartitaIva)
            .WithName("ValidatePartitaIva")
            .WithDescription("Validate a Partita IVA number")
            .Produces<ValidationResult>(200);

        return group;
    }

    private static async Task<IResult> GetAllClients(IClientRepository repository)
    {
        var clients = await repository.GetAllAsync();
        return Results.Ok(clients);
    }

    private static async Task<IResult> GetClientById(Guid id, IClientRepository repository)
    {
        var client = await repository.GetByIdAsync(id);
        return client != null ? Results.Ok(client) : Results.NotFound();
    }

    private static async Task<IResult> CreateClient(
        Client client,
        IClientRepository repository)
    {
        // Validate Partita IVA
        if (!PartitaIvaValidator.Validate(client.PartitaIva))
        {
            var error = PartitaIvaValidator.GetValidationError(client.PartitaIva);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "PartitaIva", new[] { error } }
            });
        }

        // Check if Partita IVA already exists
        var existing = await repository.GetByPartitaIvaAsync(client.PartitaIva);
        if (existing != null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "PartitaIva", new[] { "Partita IVA già esistente" } }
            });
        }

        var created = await repository.CreateAsync(client);
        return Results.Created($"/api/clients/{created.Id}", created);
    }

    private static async Task<IResult> UpdateClient(
        Guid id,
        Client client,
        IClientRepository repository)
    {
        if (id != client.Id)
        {
            return Results.BadRequest("ID mismatch");
        }

        var existing = await repository.GetByIdAsync(id);
        if (existing == null)
        {
            return Results.NotFound();
        }

        // Validate Partita IVA
        if (!PartitaIvaValidator.Validate(client.PartitaIva))
        {
            var error = PartitaIvaValidator.GetValidationError(client.PartitaIva);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "PartitaIva", new[] { error } }
            });
        }

        // Check if Partita IVA is taken by another client
        var duplicateCheck = await repository.GetByPartitaIvaAsync(client.PartitaIva);
        if (duplicateCheck != null && duplicateCheck.Id != id)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                { "PartitaIva", new[] { "Partita IVA già esistente" } }
            });
        }

        var updated = await repository.UpdateAsync(client);
        return Results.Ok(updated);
    }

    private static async Task<IResult> DeleteClient(Guid id, IClientRepository repository)
    {
        var deleted = await repository.DeleteAsync(id);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static IResult ValidatePartitaIva(string partitaIva)
    {
        var isValid = PartitaIvaValidator.Validate(partitaIva);
        var error = isValid ? null : PartitaIvaValidator.GetValidationError(partitaIva);

        return Results.Ok(new ValidationResult
        {
            IsValid = isValid,
            Error = error
        });
    }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? Error { get; init; }
}
