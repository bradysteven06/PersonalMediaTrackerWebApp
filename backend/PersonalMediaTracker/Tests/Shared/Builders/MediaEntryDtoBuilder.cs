// Purpose:
// - Provide defaults for MediaEntryDto so tests can customize just
//   what matters for the scenario.
// - Avoids duplicatiing boilerplate DTO setup across many tests.

using WebApi.Contracts;
using Domain.Enums;

namespace Tests.Shared.Builders
{
    public class MediaEntryDtoBuilder
    {
        private readonly MediaEntryDto _dto = new()
        {
            Title = "Sample Title",
            Type = EntryType.Series,        // default type
            SubType = null,                 // nullable by default
            Status = EntryStatus.Planning,  // default status
            Rating = 5.0m,                  // default Rating
            Notes = "N/A",
            Tags = new List<string>()       // ensure non-null
        };

        public MediaEntryDtoBuilder WithTitle(string title) { _dto.Title = title; return this; }
        public MediaEntryDtoBuilder withType(EntryType type) { _dto.Type = type; return this; }
        public MediaEntryDtoBuilder withSubType(EntrySubType? sub) { _dto.SubType = sub; return this; }
        public MediaEntryDtoBuilder withStatus(EntryStatus status) { _dto.Status = status; return this; }
        public MediaEntryDtoBuilder withRating(decimal? rating) { _dto.Rating = rating; return this; }
        public MediaEntryDtoBuilder withNotes(string? notes) { _dto.Notes = notes; return this; }
        public MediaEntryDtoBuilder WithTags(params string[] tags) { _dto.Tags = tags; return this; }

        public MediaEntryDto Build() => _dto;
    }
}
