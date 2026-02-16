using Fatturazione.Domain.Models;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// Repository interface for the single IssuerProfile (cedente/prestatore)
/// Art. 21, co. 2, lett. c-d, DPR 633/72
/// </summary>
public interface IIssuerProfileRepository
{
    Task<IssuerProfile?> GetAsync();
    Task<IssuerProfile> SaveAsync(IssuerProfile profile);
}
