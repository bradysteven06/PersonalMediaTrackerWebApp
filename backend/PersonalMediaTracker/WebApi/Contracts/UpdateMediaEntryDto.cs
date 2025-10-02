using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace WebApi.Contracts
{
    // Write model for PUT/PATCH-like full update
    public class UpdateMediaEntryDto
    {
        public Guid Id { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        public EntryType? Type { get; set; }

        public EntrySubType? SubType { get; set; }

        public EntryStatus? Status { get; set; }

        [Range(0, 10)]
        public int? Rating { get; set; }

        [StringLength(2000)]
        public string? Notes { get; set; }

        public IEnumerable<string>? Tags { get; set; }
    }
}
