// Purpose: ensure soft delete is applied and filtered out by global query filters.
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.Infrastructure.Database
{
    [Collection(DatabaseCollection.Name)]
    public class SoftDeleteBehaviorTests
    {
        private readonly DatabaseFixture _db;
        public SoftDeleteBehaviorTests(DatabaseFixture db) => _db = db;

        // Add tests later for soft delete & visibility.
    }
}
