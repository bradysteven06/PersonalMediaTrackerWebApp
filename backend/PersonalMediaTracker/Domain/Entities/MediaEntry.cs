
using Domain.Enums;

namespace Domain.Entities
{
    public sealed class MediaEntry : BaseEntity
    {
        public Guid UserId { get; set; }                // multi-tenant boundary
        public string Title { get; set; } = string.Empty;
        public EntryType Type { get; set; }             // movie / series
        public EntrySubType? SubType { get; set; }       // live-action / anime / manga ....
        public EntryStatus Status { get; set; }         // planning/watching/etc.
        public decimal? Rating { get; set; }               // 0-10, nullable

        public string? Notes { get; set; }

        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>(); // many-to-many
    }

}
