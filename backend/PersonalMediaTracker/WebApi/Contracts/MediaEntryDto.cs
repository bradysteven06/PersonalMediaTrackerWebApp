using Domain.Enums;

namespace WebApi.Contracts
{
    // Read model returned to the client.
    public class MediaEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public EntryType Type { get; set; }
        public EntrySubType? SubType { get; set; }
        public EntryStatus Status { get; set; }
        public decimal? Rating { get; set; }
        public string? Notes { get; set; }
        public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();
    }
}
