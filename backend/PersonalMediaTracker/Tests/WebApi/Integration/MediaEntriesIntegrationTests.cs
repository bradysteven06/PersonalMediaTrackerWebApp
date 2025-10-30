// Purpose: End-to-end CRUD tests for /api/mediaentries using the in-memory host.
// Covers:
//  - Create -> List -> Get -> Update -> Delete (soft-delete)
//  - Tag reconciliation (create/remove, case insensitive)
//  - JSON contract sanity (enums as strings, tags materialized)
// Notes:
//  - Uses WebAppFactoryFixture to host the real pipline + SQLite
//  - Uses local DTO shapes for deserialization to keep tests decoupled

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Shared.Fixtures;
using WebApi.Controllers;
using Xunit;

namespace Tests.WebApi.Integration
{
    [Collection(WebAppFactoryCollection.Name)]
    public class MediaEntriesIntegrationTests
    {
        private readonly WebAppFactoryFixture _factory;
        public MediaEntriesIntegrationTests(WebAppFactoryFixture factory) => _factory = factory;

        // Minimal read model used for test deserialization
        private sealed record MediaEntryDto(Guid Id, Guid UserId, string Title, string Type, string? SubType, string Status, decimal? Rating, string? Notes, IReadOnlyList<string> Tags);

        [Fact]
        public async Task CrudAndSoftDelete_FullHappyPath_Works()
        {
            // Arrange: authenticated client
            var client = await _factory.CreateAuthenticatedClientAsync();

            // -----CREATE-----
            var createPayload = new
            {
                title   = "The Expanse",
                type    = "Series",
                subType = "LiveAction",
                status  = "Planning",
                rating  = 8.5m,
                notes   = "Space opera",
                tags    = new[] { "SciFi", "Drama" }
            };

            var create = await client.PostAsJsonAsync("/api/mediaentries", createPayload);
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);

            var created = await create.Content.ReadFromJsonAsync<MediaEntryDto>();
            Assert.NotNull(created);
            Assert.Equal("The Expanse", created!.Title);
            Assert.Equal("Series", created.Type);
            Assert.Equal("LiveAction", created.SubType);
            Assert.Contains("scifi", created.Tags, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("drama", created.Tags, StringComparer.OrdinalIgnoreCase);

            var id = created.Id;

            // ----- LIST -----
            // default sort=updated desc, verify item appears
            var list = await client.GetFromJsonAsync<PagedResult<MediaEntryDto>>("/api/mediaentries");
            Assert.NotNull(list);
            Assert.True(list!.Total >= 1);
            Assert.Contains(list.Items, i => i.Id == id);

            // ----- GET BY ID -----
            var byId = await client.GetFromJsonAsync<MediaEntryDto>($"/api/mediaentries/{id}");
            Assert.NotNull(byId);
            Assert.Equal("The Expanse", byId!.Title);
            Assert.Equal("Series", byId.Type);
            Assert.Equal("LiveAction", byId.SubType);

            // ----- UPDATE -----
            // Change title, status, rating notes and tags (drop "Drama", and "Space")
            var updatePayload = new
            {
                id,
                title   = "The Expanse (S1)",
                status  = "Watching",
                rating  = 9.0m,
                notes   = "Greate pilot",
                tags    = new[] { "SciFi", "Space" }
            };

            var updateResp = await client.PutAsJsonAsync($"/api/mediaentries/{id}", updatePayload);
            Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);

            var updated = await updateResp.Content.ReadFromJsonAsync<MediaEntryDto>();
            Assert.NotNull(updated);
            Assert.Equal("The Expanse (S1)", updated!.Title);
            Assert.Equal("Watching", updated.Status);
            Assert.Equal(9.0m, updated.Rating);
            Assert.Contains("space", updated.Tags, StringComparer.OrdinalIgnoreCase);
            Assert.DoesNotContain(updated.Tags, t => t.Equals("drama", StringComparison.OrdinalIgnoreCase));

            // ----- DELETE (soft) -----
            var del = await client.DeleteAsync($"/api/mediaentries/{id}");
            Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

            // Ensure it no longer appears in list (global query filter + tenant scoping)
            var listAfter = await client.GetFromJsonAsync<PagedResult<MediaEntryDto>>("/api/mediaentries");
            Assert.NotNull(listAfter);
            Assert.DoesNotContain(listAfter!.Items, i => i.Id == id);

            // GET should be 404 after soft-delete
            var getDeleted = await client.GetAsync($"/api/mediaentries/{id}");
            Assert.Equal(HttpStatusCode.NotFound, getDeleted.StatusCode);
        }

        [Fact]
        public async Task List_WithFilters_ParsesEnumsAndTagCaseInsensitively()
        {
            // Arrange
            var client = await _factory.CreateAuthenticatedClientAsync();

            // Seed two entries with distinct shapes
            var a = await client.PostAsJsonAsync("/api/mediaentries", new
            {
                title   = "Planet Earth",
                type    = "Series",
                subType = "Documentary",
                status  = "Completed",
                rating  = 9.5m,
                notes   = "BBC",
                tags    = new[] { "Nature" }
            });
            a.EnsureSuccessStatusCode();

            var b = await client.PostAsJsonAsync("/api/mediaentries", new
            {
                title   = "Spirited Away",
                type    = "Movie",
                subType = "Animated",
                status  = "Completed",
                rating  = 9.0m,
                tags    = new[] { "Fantasy", "Anime" }
            });
            b.EnsureSuccessStatusCode();

            // Act + Assert: filter by type=movie (case insensitive)
            var movies = await client.GetFromJsonAsync<PagedResult<MediaEntryDto >> ("/api/mediaentries?type=movie");
            Assert.NotNull(movies);
            Assert.All(movies!.Items, i => Assert.Equal("Movie", i.Type));

            // Filter by tag=anime (case insensitive)
            var withAnime = await client.GetFromJsonAsync<PagedResult<MediaEntryDto>>("/api/mediaentries?tag=ANIME");
            Assert.NotNull(withAnime);
            Assert.Contains(withAnime!.Items, i => i.Title == "Spirited Away");
            Assert.DoesNotContain(withAnime.Items, i => i.Title == "Planet Earth");
        }

        [Fact]
        public async Task Update_WithMismatchedRouteAndBodyId_ReturnsBadRequest()
        {
            var client = await _factory.CreateAuthenticatedClientAsync();

            var created = await client.PostAsJsonAsync("/api/mediaentries", new
            {
                title   = "Mismatch Test",
                type    = "Series",
                status  = "Planning"
            });
            created.EnsureSuccessStatusCode();
            var dto = await created.Content.ReadFromJsonAsync<MediaEntryDto>();
            Assert.NotNull(dto);

            // Send a different body Id than the route Id
            var wrongId = Guid.NewGuid();
            var badUpdate = await client.PutAsJsonAsync($"/api/mediaentries/{dto!.Id}", new
            {
                id = wrongId,
                title = "Should Fail"
            });

            Assert.Equal(HttpStatusCode.BadRequest, badUpdate.StatusCode);
            var prob = await badUpdate.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Validation error", prob.GetProperty("title").GetString());
        }

        [Fact]
        public async Task Create_WithEmptyTitle_ReturnsBadRequest()
        {
            var client = await _factory.CreateAuthenticatedClientAsync();

            var bad = await client.PostAsJsonAsync("/api/mediaentries", new
            {
                title   = "   ",
                type    = "Movie",
                status  = "Planning"
            });

            Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        }
    }
}
