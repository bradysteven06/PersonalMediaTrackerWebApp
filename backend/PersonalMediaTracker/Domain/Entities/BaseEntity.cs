

namespace Domain.Entities
{
    // Common audit + soft delete fields used by all entities.
    // Kept in Domain so the contract is persistence-agnostic.
    public abstract class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Audit timestamps are always UTC to avoid timezone confusion
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }

        // Soft-delete
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAtUtc { get; set; }

        // Optimistic concurrency stamp, configured as a rowversion in EF
        public byte[]? RowVersion { get; set; }
    }
}
