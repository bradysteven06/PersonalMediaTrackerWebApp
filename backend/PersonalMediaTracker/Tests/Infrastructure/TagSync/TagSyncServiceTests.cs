// Purpose: verify tag reconciliation (create missing, remove stale, case-insensitive).
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.Infrastructure.TagSync
{
    [Collection(DatabaseCollection.Name)]
    public class TagSyncServiceTests
    {
        private readonly DatabaseFixture _db;
        public TagSyncServiceTests(DatabaseFixture db) => _db = db;

        // Add tests later for set algebra between desired/current tags.
    }
}
