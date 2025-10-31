// Abstraction for tag reconciliation so controllers can be unit tested via mocks

using Domain.Entities;

namespace WebApi.Services
{
    public interface ITagSyncService
    {
        Task SyncAsync(MediaEntry entry, IEnumerable<string> incomingNames, Guid userId, CancellationToken ct);
    }
}
