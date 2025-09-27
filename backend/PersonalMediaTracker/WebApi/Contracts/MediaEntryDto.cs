namespace WebApi.Contracts
{
    // Read model returned to the client.
    public sealed class MediaEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }            // placeholder until auth
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = default!;        // enum as string
        public string? SubType {  get; set; }
        public string Status { get; set; } = default!;     // enum as string
        public int? Rating { get; set; }
        public string? Notes { get; set; }
        public List<string> Tags { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
