using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;


namespace WebApi.Services
{
    // Syncs string tag names to EntryTags many-to-many rows.
    // Assumes entities: Tag {Id, Name }, EntryTag { EntryId, TagId, Tag, Entry }.
    public sealed class TagSyncService : ITagSyncService
    {
        private readonly AppDbContext _db;
        public TagSyncService(AppDbContext db) => _db = db;

        public async Task SyncAsync(MediaEntry entry, IEnumerable<string>? incomingTagNames, Guid userId, CancellationToken ct)
        {
            var desired = (incomingTagNames ?? Enumerable.Empty<string>())
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.ToLowerInvariant())
                    .Distinct()
                    .ToList();

            // Load existing join rows + tag names
            await _db.Entry(entry).Collection(e => e.EntryTags).Query().Include(et =>et.Tag).LoadAsync(ct);
            var current = entry.EntryTags.Select(et => et.Tag.Name.ToLowerInvariant()).ToHashSet();

            // Determine additions/removals
            var toAdd = desired.Except(current).ToList();
            var toRemove = current.Except(desired).ToList();

            // Remove stales
            if (toRemove.Count > 0)
            {
                entry.EntryTags = entry.EntryTags
                    .Where(et => !toRemove.Contains(et.Tag.Name.ToLowerInvariant()))
                    .ToList();
            }

            // Add missing, ensure Tag rows exist
            if (toAdd.Count > 0)
            {
                var existingTags = await _db.Tags.Where(t => t.UserId == userId && toAdd.Contains(t.Name.ToLower())).ToListAsync(ct);

                var existingNames = existingTags.Select(t => t.Name.ToLower()).ToHashSet();
                var newTagNames = toAdd.Except(existingNames).ToList();

                if (newTagNames.Count > 0)
                {
                    var newTags = newTagNames.Select(n => new Tag { UserId = userId, Name = n }).ToList();
                    _db.Tags.AddRange(newTags);
                    existingTags.AddRange(newTags);
                }

                foreach (var tag in existingTags)
                {
                    entry.EntryTags.Add(new EntryTag { MediaEntry = entry, Tag = tag });
                }
            }
        }
    }
}
