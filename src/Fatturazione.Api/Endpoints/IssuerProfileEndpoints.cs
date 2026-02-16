using Fatturazione.Domain.Models;
using Fatturazione.Domain.Validators;
using Fatturazione.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fatturazione.Api.Endpoints;

/// <summary>
/// Endpoints for managing the issuer profile (cedente/prestatore)
/// Art. 21, co. 2, lett. c-d, DPR 633/72
/// </summary>
public static class IssuerProfileEndpoints
{
    public static RouteGroupBuilder MapIssuerProfileEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetIssuerProfile)
            .WithName("GetIssuerProfile")
            .WithDescription("Get the issuer profile (cedente/prestatore)")
            .Produces<IssuerProfile>(200)
            .Produces(404);

        group.MapPut("/", SaveIssuerProfile)
            .WithName("SaveIssuerProfile")
            .WithDescription("Create or update the issuer profile")
            .Produces<IssuerProfile>(200)
            .Produces<ValidationProblemDetails>(400);

        return group;
    }

    private static async Task<IResult> GetIssuerProfile(IIssuerProfileRepository repository)
    {
        var profile = await repository.GetAsync();
        return profile != null ? Results.Ok(profile) : Results.NotFound();
    }

    private static async Task<IResult> SaveIssuerProfile(
        IssuerProfile profile,
        IIssuerProfileRepository repository)
    {
        // Validate profile data (Art. 21, co. 2, lett. c-d, DPR 633/72)
        var (isValid, validationErrors) = IssuerProfileValidator.Validate(profile);
        if (!isValid)
        {
            var errorDict = validationErrors.Select((e, i) => new { Key = $"Error{i}", Value = e })
                .ToDictionary(x => x.Key, x => new[] { x.Value });
            return Results.ValidationProblem(errorDict);
        }

        var saved = await repository.SaveAsync(profile);
        return Results.Ok(saved);
    }
}
