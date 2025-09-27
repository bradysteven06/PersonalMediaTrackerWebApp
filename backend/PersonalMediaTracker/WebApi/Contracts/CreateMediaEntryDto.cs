using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

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
        public string Type { get; set; } = default!;

        [Required]
        public string? SubType {  get; set; }

        [Required]
        public string Status { get; set; } = default!;

        [Range(0, 10)]
        public int? Rating { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        // Optional, if null will be treated as empty
        public List<string>? Tags { get; set; }
    }
}
