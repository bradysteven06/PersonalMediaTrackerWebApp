using Domain.Enums;

namespace WebApi.Contracts
{
    // Read model returned to the client.
    public sealed class MediaEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }            // placeholder until auth
        public string Title { get; set; } = string.Empty;
        public EntryType Type { get; set; }       
        public EntrySubType? SubType {  get; set; }
        public EntryStatus Status { get; set; }   
        public decimal? Rating { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
