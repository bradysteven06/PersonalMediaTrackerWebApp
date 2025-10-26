// Purpose: verify EF model config: relationships, precision, indexes, query filters.
// Uses DatabaseFixture to hit a real relational (SQLite in-memory) database.
// TODO: adjust namespaces for AppDbContext & entities

using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.Infrastructure.Database
{
    [Collection(DatabaseCollection.Name)]
    public class AppDbContextTests
    {
        private readonly DatabaseFixture _db;
        public AppDbContextTests(DatabaseFixture db) => _db = db; 

        // Add tests later for model configuration behaviors.
    }
}
