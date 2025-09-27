using System.ComponentModel.DataAnnotations;

namespace WebApi.Contracts
{
    // Write model for PUT/PATCH-like full update
    public class UpdateMediaEntryDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required, StringLength(200)]
        public string? Title { get; set; }

        // These are strings in the DTO so clients don't need to know the enum ints.
        // parsed/validated in the mapping layer.
        [Required]
        public string? Type { get; set; }

        [Required]
        public string? SubType { get; set; }

        [Required]
        public string? Status { get; set; }

        [Range(0, 10)]
        public int? Rating { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Optional, if null will be treated as empty
        public List<string>? Tags { get; set; }
    }
}
