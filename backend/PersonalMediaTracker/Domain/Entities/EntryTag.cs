using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    // Explicity join entity for the many-to-many between MediaEntry and Tag.
    // Lets you add extra properties later (e.g., CreatedAt) if needed.
    public sealed class EntryTag
    {
        public Guid MediaEntryId { get; set; }
        public MediaEntry MediaEntry { get; set; } = default!;

        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = default!;
    }
}
