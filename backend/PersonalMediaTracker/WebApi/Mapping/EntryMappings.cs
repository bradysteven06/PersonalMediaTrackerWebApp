using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using WebApi.Contracts;
using Microsoft.AspNetCore.Http;

namespace WebApi.Mapping
{
    // Central place to convert between DTOs and domain entities.
    // Keeps controllers small, testable, and consistent.
    public static class EntryMappings
    {
        // ------ Parsing helpers -------------------------------

        // Try parse case-insensitive enum names with nice errors.
        public static bool TryParseMediaType(string value, out MediaType parsed, out string? error)
        {
            if (Enum.TryParse<MediaType>(value, ignoreCase: true, out parsed))
            {
                error = null;
                return true;
            }
            error = $"Invalid 'Type' value '{value}'. Allowed: {string.Join(", ", Enum.GetNames(typeof(MediaType)))}.";
            return false;
        }

        public static bool TryParseEntryStatus(string value, out EntryStatus parsed, out string? error)
        { 
            if (Enum.TryParse<EntryStatus>(value, ignoreCase: true, out parsed))
            {
                error = null; 
                return true;
            }
            error = $"Invalid 'Status' value '{value}'. Allowed: {string.Join(", ", Enum.GetNames(typeof(EntryStatus)))}.";
            return false;
        }

        // ------ DTO -> Entity ------------------------------

        public static (MediaEntry entity, ProblemDetails? error) ToEntity(this EntryCreateRequest dto, Guid userId)
        {
            if (!TryParseMediaType(dto.Type, out var type, out var typeErr))
            {
                return (null!,new ProblemDetails { Title = "Validation error", Detail = typeErr, Status = StatusCodes.Status400BadRequest });
            }

            if (!TryParseEntryStatus(dto.Status, out var status, out var statusErr))
            {
                return (null!, new ProblemDetails { Title = "Validation error", Detail = statusErr, Status = StatusCodes.Status400BadRequest });
            }

            var cross = ValidateCrossFields(dto);
            if (cross is not null)
            {
                return (null!, new ProblemDetails { Title = "Validation error", Detail = cross, Status = StatusCodes.Status400BadRequest });
            }

            var entity = new MediaEntry
            {
                // Id is assigned by EF, UserId comes from auth (placeholder for now)
                UserId = userId,
                Title = dto.Title.Trim(),
                Type = type,
                Status = status,
                Rating = dto.Rating,
                Progress = dto.Progress,
                Total = dto.Total,
                StartedAt = dto.StartedAt,
                FinishedAt = dto.FinishedAt,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim()
                // EntryTags populated separately (will sync via helper)
            };

            return (entity, null);
        }

        public static ProblemDetails? ApplyTo(this EntryCreateRequest dto, MediaEntry entity)
        {
            if (!TryParseMediaType(dto.Type, out var type, out var typeErr))
            {
                return  new ProblemDetails { Title = "Validation error", Detail = typeErr, Status = StatusCodes.Status400BadRequest };
            }

            if (!TryParseEntryStatus(dto.Status, out var status, out var statusErr))
            {
                return new ProblemDetails { Title = "Validation error", Detail = statusErr, Status = StatusCodes.Status400BadRequest };
            }

            var cross2 = ValidateCrossFields(dto);
            if (cross2 is not null)
            {
                return new ProblemDetails { Title = "Validation error", Detail = cross2, Status = StatusCodes.Status400BadRequest };
            }

            entity.Title = dto.Title.Trim();
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

        public static EntryResponse ToResponse(this MediaEntry e)
        {
            return new EntryResponse
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
                Tags = e.EntryTags.Select(t => t.Tag.Name).ToList(), // assumes EntryTag.Tag.Name
                CreatedAtUtc = e.CreatedAtUtc,
                UpdatedAtUtc = e.UpdatedAtUtc
            };
        }

        // Cross-field validation
        private static string? ValidateCrossFields(EntryCreateRequest dto)
        {
            if (dto.Total is not null && dto.Progress > dto.Total)
            {
                return $"Progress ({dto.Progress}) cannot exceed Total ({dto.Total}).";
            }

            if (dto.StartedAt is not null && dto.FinishedAt is not null && dto.StartedAt > dto.FinishedAt)
            {
                return "StartedAt cannot be after FinishedAt.";
            }

            return null;
        }
    }
}
