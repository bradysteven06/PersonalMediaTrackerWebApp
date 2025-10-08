using System;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    // EF Core DbContext for Personal Media Tracker + ASP.NET Core Identity.
    // - Hosts domain entities and Identity tables
    // - Keeps audit + soft delete rules centralized
    public sealed class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // DbSets = tables you can query/update
        public DbSet<MediaEntry> MediaEntries => Set<MediaEntry>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<EntryTag> EntryTags => Set<EntryTag>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -----------------------
            // Media Entry mapping
            // -----------------------
            var entry = modelBuilder.Entity<MediaEntry>();
            entry.ToTable("MediaEntries");
            entry.HasKey(e => e.Id);

            entry.Property(e => e.Title).HasMaxLength(200).IsRequired();

            // Store enums as strings for readability. MaxLength keeps storage tight.
            entry.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            entry.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(16)
                .IsRequired();

            // Rating supports 0.0-10.0 with one decimal
            entry.Property(e => e.Rating)
                .HasPrecision(4, 1);

            // Helpful indexes for common filters
            entry.HasIndex(e => new { e.UserId, e.Type });
            entry.HasIndex(e => new { e.UserId, e.Status });
            entry.HasIndex(e => new { e.UserId, e.UpdatedAtUtc });

            // Global soft-delete filter (hides deleted rows by default)
            entry.HasQueryFilter(e => !e.IsDeleted);

            // -------------------
            // Tag mapping
            // -------------------
            var tag = modelBuilder.Entity<Tag>();
            tag.ToTable("Tags");
            tag.HasKey(t => t.Id);
            tag.Property(t => t.Name).HasMaxLength(64).IsRequired();
            tag.HasIndex(t => new { t.UserId, t.Name }).IsUnique();     // prevent duplicate tag names per user
            tag.HasQueryFilter(t => !t.IsDeleted);                      // global soft-delete filter

            // ---------------------------------
            // EntryTag (explicit join) mapping
            // ---------------------------------
            var entryTag = modelBuilder.Entity<EntryTag>();
            entryTag.ToTable("EntryTags");

            // Composite primary key ensures each (entry, tag) pair is unique
            entryTag.HasKey(et => new { et.MediaEntryId, et.TagId });

            // FK: EntryTag -> MediaEntry (many EntryTags per MediaEntry)
            entryTag.HasOne(et => et.MediaEntry).WithMany(t => t.EntryTags).HasForeignKey(et => et.MediaEntryId).OnDelete(DeleteBehavior.Cascade);

            // FK: EntryTag -> Tag (many EntryTags per Tag)
            entryTag.HasOne(et => et.Tag).WithMany(t => t.EntryTags).HasForeignKey(et => et.TagId).OnDelete(DeleteBehavior.Cascade);

            // Optional single-column indexes can help some queries
            entryTag.HasIndex(et => et.MediaEntryId);
            entryTag.HasIndex(et => et.TagId);       
            
            // Match the principals' global filters so the join never "sees" filtered principals
            entryTag.HasQueryFilter(et => !et.MediaEntry.IsDeleted && !et.Tag.IsDeleted);

            // Concurrency tokens
            modelBuilder.Entity<MediaEntry>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Tag>().Property(e => e.RowVersion).IsRowVersion();
        }

        // Audit + Soft-delete handling.
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditRules();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAuditRules();
            return base.SaveChanges();
        }

        // Sets CreatedAtUtc/UpdatedAtUtc automatically and converts hard deletes in soft deletes.
        // Keeping this here avoids duplicating audit logic in controllers/use-cases.
        private void ApplyAuditRules()
        {
            var utcNow = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    entry.Entity.IsDeleted = false;
                    entry.Entity.DeletedAtUtc = null;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedAtUtc = utcNow;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Convert hard delete -> soft delete
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = utcNow;
                }
            }
        }
    }
}
