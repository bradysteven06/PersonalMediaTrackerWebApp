using Domain.Entities;
using WebApi.Contracts;

namespace WebApi.Mapping
{
    // Central place to convert between DTOs and domain entities.
    // Keeps controllers small, testable, and consistent.
    public static class MediaEntryMappings
    {
        
        // ------ DTO -> Entity (Create)-------------------------

        public static (MediaEntry entity, string? error) ToEntity(this CreateMediaEntryDto dto, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                return (null!, "Title is required.");
            }

            var entity = new MediaEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = dto.Title.Trim(),
                Type = dto.Type,
                SubType = dto.SubType,
                Status = dto.Status,
                Rating = dto.Rating,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
                // Tags wired in separately (controller/service calls a Tag attach helper)
            };

            return (entity, null);
        }

        // ----- DTO -> Entity (Update full) --------
        public static string? ApplyTo(this UpdateMediaEntryDto dto, MediaEntry entity)
        {
            if (dto.Title is not null)
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    return "Title is required.";
                }
                entity.Title = dto.Title.Trim();
            }

            if (dto.Type.HasValue)      entity.Type = dto.Type.Value;
            if (dto.SubType.HasValue)   entity.SubType = dto.SubType.Value;
            if (dto.Status.HasValue)    entity.Status = dto.Status.Value;
            if (dto.Rating.HasValue)    entity.Rating = dto.Rating.Value;
            if (dto.Notes is not null)  entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            return null;
        }

        // ------ Entity -> DTO ------------------------------

        public static MediaEntryDto ToDto(this MediaEntry entity)
        {
            return new MediaEntryDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Title = entity.Title,
                Type = entity.Type,
                SubType = entity.SubType,
                Status = entity.Status,
                Rating = entity.Rating,                
                Notes = entity.Notes,
                Tags = (entity.EntryTags.Select(t => t.Tag!.Name))
            };
        }

        
    }
}
