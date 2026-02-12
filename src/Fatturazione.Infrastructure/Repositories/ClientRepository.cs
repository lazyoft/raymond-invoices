using Fatturazione.Domain.Models;
using Fatturazione.Infrastructure.Data;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of Client repository
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly InMemoryDataStore _dataStore;

    public ClientRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<IEnumerable<Client>> GetAllAsync()
    {
        var clients = _dataStore.Clients.Values.OrderBy(c => c.RagioneSociale).AsEnumerable();
        return Task.FromResult(clients);
    }

    public Task<Client?> GetByIdAsync(Guid id)
    {
        _dataStore.Clients.TryGetValue(id, out var client);
        return Task.FromResult(client);
    }

    public Task<Client?> GetByPartitaIvaAsync(string partitaIva)
    {
        var client = _dataStore.Clients.Values
            .FirstOrDefault(c => c.PartitaIva == partitaIva);
        return Task.FromResult(client);
    }

    public Task<Client> CreateAsync(Client client)
    {
        if (client.Id == Guid.Empty)
            client.Id = Guid.NewGuid();

        client.CreatedAt = DateTime.UtcNow;

        _dataStore.Clients.TryAdd(client.Id, client);
        return Task.FromResult(client);
    }

    public Task<Client?> UpdateAsync(Client client)
    {
        if (!_dataStore.Clients.ContainsKey(client.Id))
            return Task.FromResult<Client?>(null);

        _dataStore.Clients[client.Id] = client;
        return Task.FromResult<Client?>(client);
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        var removed = _dataStore.Clients.TryRemove(id, out _);
        return Task.FromResult(removed);
    }

    public Task<bool> ExistsAsync(Guid id)
    {
        var exists = _dataStore.Clients.ContainsKey(id);
        return Task.FromResult(exists);
    }
}
