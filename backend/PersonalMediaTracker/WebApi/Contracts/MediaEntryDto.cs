namespace WebApi.Contracts
{
    // Read model returned to the client.
    public sealed class MediaEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }            // placeholder until auth
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;        // enum as string
        public string Status { get; set; } = string.Empty;     // enum as string
        public byte? Rating { get; set; }
        public int Progress { get; set; }
        public int? Total { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string? Notes { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
