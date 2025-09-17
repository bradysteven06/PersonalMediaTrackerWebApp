
namespace Domain.Entities
{
    public sealed class Tag : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        public ICollection<EntryTag> EntryTags { get; set; } = new List<EntryTag>();
    }
}
