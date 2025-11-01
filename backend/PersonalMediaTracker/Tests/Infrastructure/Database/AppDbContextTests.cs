// Purpose: 
// - Verify EF Core model configuration in jInfrastructure.Persistence.AppDbContext:
//   - Entity keys & indexes
//   - Property config (precision, max length, concurrency tokens)
//   - Global query filters for soft-deletes
//   - Basic audit timestamps on add/update

using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.Infrastructure.Database
{
    [Collection(DatabaseCollection.Name)]
    public class AppDbContextTests
    {
        private readonly DatabaseFixture _db;
        public AppDbContextTests(DatabaseFixture db) => _db = db; 

        [Fact]
        public void Model_Metadata_IsConfigured_AsExpected()
        {
            using var ctx = _db.CreateContext();
            var model = ctx.Model;

            // ----- MediaEntry -----
            var mediaEntry = model.FindEntityType(typeof(MediaEntry))!;
            Assert.NotNull(mediaEntry);

            // Primary key
            Assert.Equal(new[] { "Id" }, mediaEntry.FindPrimaryKey()!.Properties.Select(p => p.Name));

            // Indexes configured in AppDbContext:
            //   HasIndex(e => new { e.UserId, e.Type });
            //   HasIndex(e => new { e.UserId, e.Status });
            //   HasIndex(e => new { e.UserId, e.UpdatedAtUtc });
            var meIndexProps = mediaEntry.GetIndexes().Select(ix => string.Join(",", ix.Properties.Select(p => p.Name))).ToArray();
            Assert.Contains("UserId,Type", meIndexProps);
            Assert.Contains("UserId,Status", meIndexProps);
            Assert.Contains("UserId,UpdatedAtUtc", meIndexProps);

            // Enum columns have MaxLength(16) (stored as string via HasConversion<string>)
            Assert.Equal(16, mediaEntry.FindProperty("Type")!.GetMaxLength());  
            Assert.Equal(16, mediaEntry.FindProperty("Status")!.GetMaxLength()); 

            // Rating precision(4,1)
            Assert.Equal(4, mediaEntry.FindProperty("Rating")!.GetPrecision());  
            Assert.Equal(1, mediaEntry.FindProperty("Rating")!.GetScale());      

            // RowVersion is a concurrency token
            Assert.True(mediaEntry.FindProperty("RowVersion")!.IsConcurrencyToken); 

            // ---- Tag ----
            var tag = model.FindEntityType(typeof(Tag))!;
            Assert.NotNull(tag);

            // Unique index (UserId, Name)
            var tagIndexes = tag.GetIndexes().ToArray();
            var userNameIx = tagIndexes.Single(ix => ix.Properties.Select(p => p.Name).SequenceEqual(new[] { "UserId", "Name" }));
            Assert.True(userNameIx.IsUnique); 

            // Name MaxLength(64)
            Assert.Equal(64, tag.FindProperty("Name")!.GetMaxLength()); 

            // ---- EntryTag (join) ----
            var entryTag = model.FindEntityType(typeof(EntryTag))!;
            Assert.NotNull(entryTag);

            // Composite PK (MediaEntryId, TagId)
            Assert.Equal(new[] { "MediaEntryId", "TagId" }, entryTag.FindPrimaryKey()!.Properties.Select(p => p.Name)); 

            // Indexes on FKs
            var etIndexProps = entryTag.GetIndexes().Select(ix => string.Join(",", ix.Properties.Select(p => p.Name))).ToArray();
            Assert.Contains("MediaEntryId", etIndexProps);
            Assert.Contains("TagId", etIndexProps); 
        }

        [Fact]
        public async Task GlobalQueryFilters_HideSoftDeleted_Rows()
        {
            _db.Reset();
            using var ctx = _db.CreateContext();

            var user = Guid.NewGuid();

            var aliveEntry = new MediaEntry { UserId = user, Title = "Alive", Status = EntryStatus.Planning, Type = EntryType.Movie };
            var deletedEntry = new MediaEntry { UserId = user, Title = "Trash", Status = EntryStatus.Planning, Type = EntryType.Movie };

            var aliveTag = new Tag { UserId = user, Name = "alive" };
            var deletedTag = new Tag { UserId = user, Name = "trash" };

            ctx.AddRange(aliveEntry, deletedEntry, aliveTag, deletedTag);
            await ctx.SaveChangesAsync(); // audit timestamps set here (CreatedAtUtc/UpdatedAtUtc)

            // Soft-delete one entry + one tag (Remove -> SaveChanges converts to soft-delete)
            ctx.Remove(deletedEntry);
            ctx.Remove(deletedTag);
            await ctx.SaveChangesAsync(); // SaveChanges override flips IsDeleted + DeletedAtUtc

            // Query without IgnoreQueryFilters -> only non-deleted rows visible
            var entriesVisible = await ctx.MediaEntries.AsNoTracking().CountAsync();
            var tagsVisible = await ctx.Tags.AsNoTracking().CountAsync();
            Assert.Equal(1, entriesVisible); // filter: e => !e.IsDeleted
            Assert.Equal(1, tagsVisible);    // filter: t => !t.IsDeleted

            // With IgnoreQueryFilters -> both rows should be present
            var entriesAll = await ctx.MediaEntries.IgnoreQueryFilters().CountAsync();
            var tagsAll = await ctx.Tags.IgnoreQueryFilters().CountAsync();
            Assert.Equal(2, entriesAll);
            Assert.Equal(2, tagsAll);
        }

        [Fact]
        public async Task EntryTag_Filter_Mirrors_Principals_SoftDelete()
        {
            _db.Reset();

            // Seed principals in one context
            using (var ctx = _db.CreateContext())
            {
                var user = Guid.NewGuid();

                var entry = new MediaEntry
                {
                    UserId = user,
                    Title = "E",
                    Status = EntryStatus.Planning,
                    Type = EntryType.Series
                };
                var tag = new Tag { UserId = user, Name = "scifi" };

                ctx.AddRange(entry, tag);
                await ctx.SaveChangesAsync();

                // Create the join using explicit FK keys (important for composite PK)
                var join = new EntryTag
                {
                    MediaEntryId = entry.Id,
                    TagId = tag.Id,
                    MediaEntry = entry,
                    Tag = tag
                };

                ctx.EntryTags.Add(join);
                await ctx.SaveChangesAsync();

                // Clear tracking so the next context sees the DB state fresh
                ctx.ChangeTracker.Clear();
            }

            // Verify in a fresh context (avoids identity-map / fix-up wrinkles)
            using (var verify = _db.CreateContext())
            {
                // Sanity: row exists physically
                Assert.Equal(1, await verify.EntryTags.IgnoreQueryFilters().CountAsync());

                // With filters ON, join should be visible because both principals are not deleted
                Assert.Equal(1, await verify.EntryTags.CountAsync());

                // 3) Soft-delete Tag -> join should disappear due to EntryTag filter
                var theTag = await verify.Tags.SingleAsync();  // only one we inserted
                verify.Remove(theTag);
                await verify.SaveChangesAsync();

                var visibleJoins = await verify.EntryTags.CountAsync();
                Assert.Equal(0, visibleJoins); // hidden by filter

                // Under the hood row still exists
                var allJoins = await verify.EntryTags.IgnoreQueryFilters().CountAsync();
                Assert.Equal(1, allJoins);
            }
        }

        [Fact]
        public async Task Audit_Timestamps_Set_OnAdd_And_Update()
        {
            _db.Reset();
            using var ctx = _db.CreateContext();

            var tag = new Tag { UserId = Guid.NewGuid(), Name = "first" };
            ctx.Tags.Add(tag);
            await ctx.SaveChangesAsync(); // ApplyAuditRules on Add sets CreatedAtUtc/UpdatedAtUtc

            Assert.True(tag.CreatedAtUtc != default);
            Assert.True(tag.UpdatedAtUtc != default);

            var firstUpdated = tag.UpdatedAtUtc;

            // Modify -> UpdatedAtUtc should change on SaveChanges
            tag.Name = "second";
            await ctx.SaveChangesAsync(); // ApplyAuditRules on Modified updates UpdatedAtUtc

            Assert.True(tag.UpdatedAtUtc > firstUpdated);
        }
    }
}
