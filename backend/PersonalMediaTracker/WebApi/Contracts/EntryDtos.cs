using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts
{
    // Shape returned to clients. Keep it sealed to stabilize the contract
    public sealed class EntryResponse
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

    // Create request (POST). client provides content, server assigns Id
    public class EntryCreateRequest
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // These are strings in the DTO so clients don't need to know the enum ints.
        // parsed/validated in the mapping layer.
        [Required]
        public string Type { get; set; } = "Anime"; // matches enum names: Anime/Manga/Movie/Tv
        [Required]
        public string Status { get; set; } = "Planning";

        [Range(0,10)]
        public byte? Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int Progress { get; set; }

        [Range(0, int.MaxValue)]
        public int? Total { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Optional, if null will be treated as empty
        public List<string>? Tags { get; set; }
    }

    // Update request (PUT). full update, same fields as create + Id
    public class EntryUpdateRequest : EntryCreateRequest 
    {
        [Required]
        public Guid Id { get; set; }
    }
}
