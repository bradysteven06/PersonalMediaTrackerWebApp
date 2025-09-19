using Domain.Entities;
using WebApi.Contracts;

namespace WebApi.Mapping
{
    // Central place to convert between DTOs and domain entities.
    // Keeps controllers small, testable, and consistent.
    public static class MediaEntryMappings
    {
        // normalize strings and treat whitespace as null
        private static string? NullIfWhiteSpace(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        // ---- Cached enum name lists for readable error messages ----
        private static readonly string AllowedMediaTypes = string.Join(", ", Enum.GetNames(typeof(MediaType)));
        private static readonly string AllowedStatuses = string.Join(", ", Enum.GetNames(typeof(EntryStatus)));

        // ------ Parsing helpers -------------------------------

        // Try parse case-insensitive enum names with friendly errors.
        public static bool TryParseMediaType(string value, out MediaType parsed, out string? error)
        {
            var v = NullIfWhiteSpace(value);
            if (v == null)
            {
                error = "Type is required.";
                parsed = default;
                return false;
            }

            if (Enum.TryParse<MediaType>(v, ignoreCase: true, out parsed))
            {
                error = null;
                return true;
            }
            error = $"Invalid 'Type' value '{value}'. Allowed: {AllowedMediaTypes}.";
            return false;
        }

        public static bool TryParseEntryStatus(string value, out EntryStatus parsed, out string? error)
        {
            var v = NullIfWhiteSpace(value);
            if (v == null)
            {
                error = "Status is required.";
                parsed = default;
                return false;
            }

            if (Enum.TryParse<EntryStatus>(v, ignoreCase: true, out parsed))
            {
                error = null; 
                return true;
            }
            error = $"Invalid 'Status' value '{value}'. Allowed: {AllowedStatuses}.";
            return false;
        }

        // --------- Cross-field validation shared by create/update ----------
        private static string? ValidateCrossFields(string title, int? progress, int? total, DateTime? started, DateTime? finished)
        {
            if (string.IsNullOrWhiteSpace(title)) return "Title is required.";
            if (total is not null && progress > total) return $"Progress ({progress}) cannot exceed Total ({total}).";
            if (started is not null && finished is not null && started > finished) return "StartedAt cannot be after FinishedAt.";
            return null;
        }

        // ------ DTO -> Entity (Create)-------------------------

        public static (MediaEntry entity, string? error) ToEntity(this CreateMediaEntryDto dto, Guid userId)
        {
            if (!TryParseMediaType(dto.Type, out var type, out var typeErr)) return (null!, typeErr);
            if (!TryParseEntryStatus(dto.Status, out var status, out var statusErr)) return (null!, statusErr);

            var normalizedTitle = dto.Title?.Trim() ?? string.Empty;
            var cross = ValidateCrossFields(normalizedTitle, dto.Progress, dto.Total, dto.StartedAt, dto.FinishedAt);
            if (cross is not null) return (null!, cross);

            var entity = new MediaEntry
            {
                UserId = userId,
                Title = normalizedTitle,
                Type = type,
                Status = status,
                Rating = dto.Rating,
                Progress = dto.Progress,
                Total = dto.Total,
                StartedAt = dto.StartedAt,
                FinishedAt = dto.FinishedAt,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
                // Tags wired in separately (controller/service calls a Tag attach helper)
            };

            return (entity, null);
        }

        // ----- DTO -> Entity (Update full) --------
        public static string? ApplyTo(this UpdateMediaEntryDto dto, MediaEntry entity)
        {
            if (!TryParseMediaType(dto.Type, out var type, out var typeErr)) return typeErr;
            if (!TryParseEntryStatus(dto.Status, out var status, out var statusErr)) return statusErr;

            var normalizedTitle = dto.Title?.Trim() ?? string.Empty;
            var cross = ValidateCrossFields(normalizedTitle, dto.Progress, dto.Total, dto.StartedAt, dto.FinishedAt);
            if (cross is not null) return cross;

            entity.Title = normalizedTitle;
            entity.Type = type;
            entity.Status = status;
            entity.Rating = dto.Rating;
            entity.Progress = dto.Progress;
            entity.Total = dto.Total;
            entity.StartedAt = dto.StartedAt;
            entity.FinishedAt = dto.FinishedAt;
            entity.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();

            return null;
        }

        // ------ Entity -> DTO ------------------------------

        public static MediaEntryDto ToResponse(this MediaEntry e)
        {
            return new MediaEntryDto
            {
                Id = e.Id,
                UserId = e.UserId,
                Title = e.Title,
                Type = e.Type.ToString(),
                Status = e.Status.ToString(),
                Rating = e.Rating,
                Progress = e.Progress,
                Total = e.Total,
                StartedAt = e.StartedAt,
                FinishedAt = e.FinishedAt,
                Notes = e.Notes,
                Tags = (e.EntryTags?.Select(t => t.Tag.Name) ?? Enumerable.Empty<string>()).ToList(),       // Null-safe access in case Include was missed
                CreatedAtUtc = e.CreatedAtUtc,
                UpdatedAtUtc = e.UpdatedAtUtc
            };
        }

        
    }
}
