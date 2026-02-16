using Fatturazione.Domain.Models;
using Fatturazione.Infrastructure.Data;

namespace Fatturazione.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IssuerProfile repository.
/// Stores a single profile (the invoicing entity / cedente/prestatore).
/// </summary>
public class IssuerProfileRepository : IIssuerProfileRepository
{
    private readonly InMemoryDataStore _dataStore;

    public IssuerProfileRepository(InMemoryDataStore dataStore)
    {
        _dataStore = dataStore;
    }

    public Task<IssuerProfile?> GetAsync()
    {
        return Task.FromResult(_dataStore.IssuerProfile);
    }

    public Task<IssuerProfile> SaveAsync(IssuerProfile profile)
    {
        if (profile.Id == Guid.Empty)
            profile.Id = Guid.NewGuid();

        _dataStore.IssuerProfile = profile;
        return Task.FromResult(profile);
    }
}
