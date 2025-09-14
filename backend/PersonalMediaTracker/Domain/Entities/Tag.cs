using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public sealed class Tag : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<MediaEntry> Entries { get; set; } = new List<MediaEntry>();
    }
}
