// Verifies TagSyncService behavior against a real EF Core relational DB
// (SQLite in-memory via DatabaseFixture). These are INTEGRATION tests:
// - Case insensitive tag reconciliation
// - Deduplication of incoming names
// - Creation of missing Tag rows per user
// - Removal of stale EntryTag rows
// - Respect of unique index (UserId, Name) to avoid duplicates
// 
// Requires:
//   - AppDbContext model: Tag unique index (UserId, Name)
//   - TagSyncService wired to the same DbContext

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tests.Shared.Fixtures;
using WebApi.Services;
using Xunit;

namespace Tests.Infrastructure.TagSync
{
    [Collection(DatabaseCollection.Name)]
    public class TagSyncServiceTests
    {
        private readonly DatabaseFixture _db;
        public TagSyncServiceTests(DatabaseFixture db) => _db = db;

        private static MediaEntry NewEntry(Guid userId) => new MediaEntry
        {
            UserId  = userId,
            Title   = "Entry",
            Status  = EntryStatus.Planning,
            Type    = EntryType.Series
        };

        [Fact]
        public async Task SyncAsync_AddsMissingTags_And_CreatesEntryTagJoins()
        {
            _db.Reset();
            using var ctx = _db.CreateContext();
            var svc = new TagSyncService(ctx);
            var userId = Guid.NewGuid();

            var entry = NewEntry(userId);
            ctx.MediaEntries.Add(entry);
            await ctx.SaveChangesAsync();

            // incoming tags (mixed case, duplicates)
            var incoming = new[] { "Action", "action", "Drama" };

            // Act
            await svc.SyncAsync(entry, incoming, userId, CancellationToken.None);
            await ctx.SaveChangesAsync();

            // Assert: Tags created (deduped and lower-cased by service), join rows added
            var tags = await ctx.Tags.Where(t => t.UserId == userId).ToListAsync();
            Assert.Equal(2, tags.Count);
            Assert.Contains(tags, t => t.Name == "action");
            Assert.Contains(tags, t => t.Name == "drama");

            // Entry has two joins
            Assert.Equal(2, entry.EntryTags.Count);
            Assert.Equal(2, await ctx.EntryTags.CountAsync());
        }

        [Fact]
        public async Task SyncAsync_RemovesStaleJoins_WhenTagNoLongerDesired()
        {
            // Arrange
            _db.Reset();
            using var ctx = _db.CreateContext();
            var svc = new TagSyncService(ctx);
            var userId = Guid.NewGuid();

            var action = new Tag { UserId = userId, Name = "action" };
            var drama = new Tag { UserId = userId, Name = "drama" };
            ctx.Tags.AddRange(action, drama);

            var entry = NewEntry(userId);
            entry.EntryTags.Add(new EntryTag { MediaEntry = entry, Tag = action });
            entry.EntryTags.Add(new EntryTag { MediaEntry = entry, Tag = drama });

            ctx.MediaEntries.Add(entry);
            await ctx.SaveChangesAsync();

            // Keep only "action"
            var incoming = new[] { "Action" };

            //Act
            await svc.SyncAsync(entry, incoming, userId, CancellationToken.None);
            await ctx.SaveChangesAsync();

            // Assert: "drama" should be removed; "action" remains
            var refreshed = await ctx.MediaEntries
                .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
                .FirstAsync(e => e.Id == entry.Id);

            Assert.Single(refreshed.EntryTags);
            Assert.Equal("action", refreshed.EntryTags.Single().Tag.Name);
        }

        [Fact]
        public async Task SyncAsync_IsCaseInsensitive_And_DoesNotDuplicateExistingTags()
        {
            // Arrange
            _db.Reset();
            using var ctx = _db.CreateContext();
            var svc = new TagSyncService(ctx);
            var userId = Guid.NewGuid();

            // Existing tag "scifi"
            var scifi = new Tag { UserId = userId, Name = "scifi" };
            ctx.Tags.Add(scifi);

            var entry = NewEntry(userId);
            ctx.MediaEntries.Add(entry);
            await ctx.SaveChangesAsync();

            // Incoming with different case - should map to same Tag row
            var incoming = new[] { "SciFi" };

            // Act
            await svc.SyncAsync(entry, incoming, userId, CancellationToken.None);
            await ctx.SaveChangesAsync();

            // Assert: still a single Tag row for user, one join
            var tags = await ctx.Tags.Where(t => t.UserId == userId).ToListAsync();
            Assert.Single(tags);
            Assert.Equal("scifi", tags[0].Name);

            var joinCount = await ctx.EntryTags.CountAsync();
            Assert.Equal(1, joinCount);
        }

        [Fact]
        public async Task SyncAsync_RespectsPerUserUniqueness_ForSameTagNamesAcrossUsers()
        {
            // Arrange
            _db.Reset();
            using var ctx = _db.CreateContext();
            var svc = new TagSyncService(ctx);
            var userA = Guid.NewGuid();
            var userB = Guid.NewGuid();

            // User A has "fantasy"; user B does not
            ctx.Tags.Add(new Tag { UserId = userA, Name = "fantasy" });
            await ctx.SaveChangesAsync();

            var entryB = NewEntry(userB);
            ctx.MediaEntries.Add(entryB);
            await ctx.SaveChangesAsync();

            // User B incoming "Fantasy" should create a new Tag (separate tenant)
            var incoming = new[] { "Fantasy" };

            // Act
            await svc.SyncAsync(entryB, incoming, userB, CancellationToken.None);
            await ctx.SaveChangesAsync();

            // Assert: two Tag rows exist (one per user), and userB has a join
            var tagsA = await ctx.Tags.Where(t => t.UserId == userA).ToListAsync();
            var tagsB = await ctx.Tags.Where(t => t.UserId == userB).ToListAsync();

            Assert.Single(tagsA);
            Assert.Single(tagsB);
            Assert.Equal("fantasy", tagsA[0].Name);
            Assert.Equal("fantasy", tagsB[0].Name);

            var entryBLoaded = await ctx.MediaEntries
                .Include(e => e.EntryTags).ThenInclude(et => et.Tag)
                .FirstAsync(e => e.Id == entryB.Id);

            Assert.Single(entryBLoaded.EntryTags);
            Assert.Equal(tagsB[0].Id, entryBLoaded.EntryTags.Single().TagId);
        }

        [Fact]
        public async Task SyncAsync_DeduplicatesIncomingNames_BeforeWrite()
        {
            // Arrange
            _db.Reset();
            using var ctx = _db.CreateContext();
            var svc = new TagSyncService(ctx);
            var userId = Guid.NewGuid();

            var entry = NewEntry(userId);
            ctx.MediaEntries.Add(entry);
            await ctx.SaveChangesAsync();

            // Incoming contains duplicates and mixed case
            var incoming = new[] { "Action", "action", "ACTION", "Drama" };

            // Act
            await svc.SyncAsync(entry, incoming, userId, CancellationToken.None);
            await ctx.SaveChangesAsync();

            // Assert: only two Tag rows and two joins
            Assert.Equal(2, await ctx.Tags.Where(t => t.UserId == userId).CountAsync());
            Assert.Equal(2, entry.EntryTags.Count());
        }
    }
}
