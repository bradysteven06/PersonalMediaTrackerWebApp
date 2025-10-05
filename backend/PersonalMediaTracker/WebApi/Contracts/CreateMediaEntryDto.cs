using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using System.Diagnostics.Contracts;

namespace WebApi.Contracts
{
    // Write model for POST.
    public class CreateMediaEntryDto
    {
        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public EntryType Type { get; set; }

        public EntrySubType? SubType {  get; set; }

        public EntryStatus Status { get; set; }

        [Range(0, 10, ErrorMessage = "Rating must be between 0 and 10")]
        public decimal? Rating { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public IEnumerable<string>? Tags { get; set; }
    }
}
