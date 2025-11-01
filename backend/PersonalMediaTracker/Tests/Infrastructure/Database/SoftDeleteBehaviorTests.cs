// Purpose:
// - Prove AppDbContext's soft-delete behavior and visibility semantics:
//   - Hard deletes of BaseEntity types (MediaEntry/Tag) are converted to soft deletes.
//   - Soft-deleted rows are hidden by global query filters by default.
//   - EntryTag join rows remain physically present when a principal is soft-deleted,
//      but they are hidden by EntryTag's query filter (which mirrors principals).


using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.Infrastructure.Database
{
    [Collection(DatabaseCollection.Name)]
    public class SoftDeleteBehaviorTests
    {
        private readonly DatabaseFixture _db;
        public SoftDeleteBehaviorTests(DatabaseFixture db) => _db = db;

        // Helper: seed a single MediaEntry (alive)
        private static MediaEntry NewEntry(Guid userId, string title = "Seed")
            => new MediaEntry
            {
                UserId = userId,
                Title = title,
                Type = EntryType.Movie,
                Status = EntryStatus.Planning
            };

        [Fact]
        public async Task Remove_MediaEntry_ConvertsToSoftDelete_And_HidesViaFilter()
        {
            _db.Reset();

            Guid id;
            DateTime createdAt, firstUpdated;

            // Seed + capture audit stamps
            using (var ctx = _db.CreateContext())
            {
                var e = NewEntry(Guid.NewGuid(), "Soft me");
                ctx.MediaEntries.Add(e);
                await ctx.SaveChangesAsync();

                id = e.Id;
                createdAt = e.CreatedAtUtc;
                firstUpdated = e.UpdatedAtUtc;

                // Act: "delete" -> should be converted to soft-delete in SaveChanges
                ctx.MediaEntries.Remove(e);
                await ctx.SaveChangesAsync();
            }

            // Verify in a fresh context
            using (var verify = _db.CreateContext())
            {
                // Hidden by global filter
                Assert.Null(await verify.MediaEntries.SingleOrDefaultAsync(x => x.Id == id));

                // But still exists in DB (IgnoreQueryFilters)
                var raw = await verify.MediaEntries.IgnoreQueryFilters().SingleAsync(x => x.Id == id);
                Assert.True(raw.IsDeleted);
                Assert.NotNull(raw.DeletedAtUtc);
                Assert.True(raw.CreatedAtUtc == createdAt);
                Assert.True(raw.UpdatedAtUtc >= firstUpdated); // was touched during soft-delete
            }
        }

        [Fact]
        public async Task Remove_Tag_ConvertsToSoftDelete_And_HidesViaFilter()
        {
            _db.Reset();

            Guid tagId;

            using (var ctx = _db.CreateContext())
            {
                var tag = new Tag { UserId = Guid.NewGuid(), Name = "temp" };
                ctx.Tags.Add(tag);
                await ctx.SaveChangesAsync();

                tagId = tag.Id;

                ctx.Tags.Remove(tag);
                await ctx.SaveChangesAsync();
            }

            using (var verify = _db.CreateContext())
            {
                // Hidden by filter
                Assert.Null(await verify.Tags.SingleOrDefaultAsync(t => t.Id == tagId));

                // Still present, soft-deleted
                var raw = await verify.Tags.IgnoreQueryFilters().SingleAsync(t => t.Id == tagId);
                Assert.True(raw.IsDeleted);
                Assert.NotNull(raw.DeletedAtUtc);
            }
        }

        [Fact]
        public async Task SoftDelete_Principal_Hides_EntryTag_Join_But_DoesNotPhysicallyRemoveIt()
        {
            _db.Reset();

            Guid entryId, tagId;

            // Seed entry + tag + join
            using (var ctx = _db.CreateContext())
            {
                var user = Guid.NewGuid();
                var entry = NewEntry(user, "Joined");
                var tag = new Tag { UserId = user, Name = "scifi" };

                ctx.AddRange(entry, tag);
                await ctx.SaveChangesAsync();

                ctx.EntryTags.Add(new EntryTag
                {
                    MediaEntryId = entry.Id,
                    TagId = tag.Id,
                    MediaEntry = entry,
                    Tag = tag
                });
                await ctx.SaveChangesAsync();

                entryId = entry.Id;
                tagId = tag.Id;
            }

            // Prove join exists (unfiltered)
            using (var verify1 = _db.CreateContext())
            {
                Assert.Equal(1, await verify1.EntryTags.IgnoreQueryFilters().CountAsync());
                Assert.Equal(1, await verify1.EntryTags.CountAsync());
            }

            // Soft-delete Tag (principal)
            using (var ctx = _db.CreateContext())
            {
                var tag = await ctx.Tags.SingleAsync(t => t.Id == tagId);
                ctx.Remove(tag);
                await ctx.SaveChangesAsync();
            }

            // After soft-delete:
            using (var verify2 = _db.CreateContext())
            {
                // Join row is still in DB…
                Assert.Equal(1, await verify2.EntryTags.IgnoreQueryFilters().CountAsync());

                // …but hidden by EntryTag's query filter because Tag is soft-deleted
                Assert.Equal(0, await verify2.EntryTags.CountAsync());

                // MediaEntry is still visible (not deleted)
                Assert.NotNull(await verify2.MediaEntries.SingleOrDefaultAsync(e => e.Id == entryId));
            }
        }
    }
}
