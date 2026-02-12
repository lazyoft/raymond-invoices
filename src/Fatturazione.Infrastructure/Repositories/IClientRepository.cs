using Fatturazione.Domain.Models;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// Repository interface for Client entity
/// </summary>
public interface IClientRepository
{
    Task<IEnumerable<Client>> GetAllAsync();
    Task<Client?> GetByIdAsync(Guid id);
    Task<Client?> GetByPartitaIvaAsync(string partitaIva);
    Task<Client> CreateAsync(Client client);
    Task<Client?> UpdateAsync(Client client);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
