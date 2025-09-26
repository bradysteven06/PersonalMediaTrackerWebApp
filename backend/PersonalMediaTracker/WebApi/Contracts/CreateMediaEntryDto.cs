using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts
{
    // Write model for POST.
    public class CreateMediaEntryDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        // These are strings in the DTO so clients don't need to know the enum ints.
        // parsed/validated in the mapping layer.
        [Required]
        public string Type { get; set; } = "Anime"; // matches enum names: Anime/Manga/Movie/Tv
        [Required]
        public string Status { get; set; } = "Planning";

        [Range(0, 10)]
        public byte? Rating { get; set; }

        [Range(0, int.MaxValue)]
        public int? Progress { get; set; }

        [Range(0, int.MaxValue)]
        public int? Total { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Optional, if null will be treated as empty
        public List<string>? Tags { get; set; }
    }
}
