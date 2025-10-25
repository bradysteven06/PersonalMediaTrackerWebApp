// Purpose:
// - Creates a single in-memory SQLite connection that stays open for
//   the liftime of the fixture (xUnit collection).
// - Exposes a factory method to create AppDbContext instances that all
//   share the same database (useful for Arrange/Act contexts).
// - Provides Reset() to clear tables between tests when needed.

using System;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Persistence;

namespace Tests.Shared.Fixtures
{
    public sealed class DatabaseFixture : IDisposable
    {
        // Keep connection open so the in-memory database persists across contexts.
        private readonly SqliteConnection _connection;

        // Create options. Each context uses the same connection.
        private readonly DbContextOptions<AppDbContext> _options;

        public DatabaseFixture()
        {
            // Create the special in-memory connection string.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            // Build DbContext options bound to the same open connection.
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .EnableSensitiveDataLogging() // helpful for debugging tests
                .Options;

            // Ensure schema exists (no migrations needed for tests).
            using var db = CreateContext();
            db.Database.EnsureCreated();
        }

        // Create a new AppDbContext that uses the shared in-memory database.
        // Each call returns a fresh, disposable context for a test.
        public AppDbContext CreateContext() => new AppDbContext(_options);

        // Quickly clear data between tests sharing the same fixture.
        // Use when test isolation requires a clean slate.
        public void Reset()
        {
            using var db = CreateContext();
            // Truncate/clean tables as needed. For simple cases, re-create schema
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }

    // xUnit collection to share the fixture across multiple test classes that
    // need the same database instance (avoid re-creating/opening per class).
    [Xunit.CollectionDefinition(Name)]
    public class DatabaseCollection : Xunit.ICollectionFixture<DatabaseFixture>
    {
        public const string Name = "DatabaseCollection";
        // Intentionally empty. the attribure wires the fixture into xUnit.
    }
}
