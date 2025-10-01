namespace WebApi.Contracts
{
    // Read model returned to the client.
    public sealed class MediaEntryDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }            // placeholder until auth
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = default!;        
        public string? SubType {  get; set; }
        public string Status { get; set; } = default!;     
        public int? Rating { get; set; }
        public string? Notes { get; set; }
        public IEnumerable<string> Tags { get; set; } = Array.Empty<string>();
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
