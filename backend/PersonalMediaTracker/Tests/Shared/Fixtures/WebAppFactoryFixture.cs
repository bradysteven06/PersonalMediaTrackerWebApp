// Purpose:
// - Hosts the real WebApi pipline in memory using WebApplicationFactory<T>.
// - Replaces the production DbContext registration with the in-memory SQLite
//   connection so API integration tests hit a realistic relational database.
// - Provides helper methods to create HttpClient instances (optionally with JWT).

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;
using Infrastructure.Persistence;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Json;

namespace Tests.Shared.Fixtures
{
    public sealed class WebAppFactoryFixture : WebApplicationFactory<Program>, IDisposable
    {
        private readonly SqliteConnection _connection;

        public WebAppFactoryFixture()
        {
            // Keep a single SQLite in-memory connection open for the app lifetime.
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // Remove existing DbContextOptions registration (e.g., SQL Server).
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

                // Register AppDbContext with the shared in-memory SQLite connection.
                services.AddDbContext<AppDbContext>(opts =>
                {
                    opts.UseSqlite(_connection);
                    opts.EnableSensitiveDataLogging();
                });

                // Build a scope and ensure schema exists for the API host.
                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();

                // disable background hosted services if your app reisters any.
                services.RemoveAll<IHostedService>();
            });            
        }

        // Create a basic HttpClient for API tests.
        public HttpClient CreateClientPlain() => CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Register + login a throwaway test user and return an HttpClient
        // with the Authoriztion header already set.
        public async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "tester@mailtest.com", string password = "Passw0rd!")
        {
            var client = CreateClientPlain();

            // Register
            await client.PostAsJsonAsync("/api/auth/register", new { email, password });

            // Login
            var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
            loginResp.EnsureSuccessStatusCode();

            var json = await loginResp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("token").GetString();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return client;
        }

        public new void Dispose()
        {
            base.Dispose();
            _connection.Dispose();
        }
    }

    // xUnit collection so multiple integration test classes reuse the same server.
    [Xunit.CollectionDefinition(Name)]
    public class WebAppFactoryCollection : Xunit.ICollectionFixture<WebAppFactoryFixture>
    {
        public const string Name = "WebAppFactoryCollection";
    }
}
