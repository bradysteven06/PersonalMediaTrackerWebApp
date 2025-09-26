
namespace Domain.Entities
{
    public sealed class MediaEntry : BaseEntity
    {
        public Guid UserId { get; set; }                // multi-tenant boundary
        public string Title { get; set; } = string.Empty;
        public MediaType Type { get; set; }             // anime/manga/movie/tv
        public EntryStatus Status { get; set; }         // planning/watching/etc.
        public byte? Rating { get; set; }               // 0-10, nullable
        public int? Progress { get; set; }               // episodes/chapters seen
        public int? Total { get; set; }                 // total episodes or chapters, if known

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? Notes { get; set; }

        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>(); // many-to-many
    }

    public enum MediaType { Anime, Manga, Movie, Tv }
    public enum EntryStatus {  Planning, Watching, Completed, OnHold, Dropped }
}
