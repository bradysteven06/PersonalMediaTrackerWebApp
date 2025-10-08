using System.Security.Claims;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApi.Contracts;
using WebApi.Mapping;
using WebApi.Services;

// CRUD + list endpoints for MediaEntry with filtering/sorting/paging and tag sync.
// Notes:
// - Uses DTOs (CreateMediaEntryDto, UpdateMediaEntryDto, MediaEntryDto) and mappers.
// - Respects multi-tenancy via UserId (stubbed helper, replace with real auth when ready). --TODO--
// - Uses TagSyncService to attach/detach many-to-many tag rows.

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public sealed class MediaEntriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly TagSyncService _tagSync;

        public MediaEntriesController(AppDbContext db, TagSyncService tagSync)
        {
            _db = db;
            _tagSync = tagSync;
        }

        // GET: api/mediaentries
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<MediaEntryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> List(
            [FromQuery] string? q,
            [FromQuery] string? type,
            [FromQuery] string? subType,
            [FromQuery] string? status,
            [FromQuery] string? tag,
            [FromQuery] string sort = "updated",
            [FromQuery] string dir = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            // Normalize paging
            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;

            var userId = GetUserId(); // user id from JWT

            // Base query, tenant-scoped, include tags for mapping
            IQueryable<Domain.Entities.MediaEntry> query = _db.MediaEntries
                .AsNoTracking()
                .Where(e => e.UserId == userId)
                .Include(e => e.EntryTags)!.ThenInclude(et => et.Tag); // mapping reads Tag.Name

            // Text search (title/notes). SQL Server is usually case-insensitive, normalize anyway.
            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(e =>
                    EF.Functions.Like(e.Title, $"%{term}%") ||
                    (e.Notes != null && EF.Functions.Like(e.Notes, $"%{term}%")));
            }

            // Enum filters: parse strings into real enums with ignoreCase=true
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<Domain.Enums.EntryType>(type.Trim(), true, out var parsedType))
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "invalid 'type' filter',",
                        Detail = $"'{type}' is not a valid EntryType."
                    });
                }
                query = query.Where(e => e.Type == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(subType))
            {
                if (!Enum.TryParse<Domain.Enums.EntrySubType>(subType.Trim(), true, out var parsedSubType))
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "invalid 'subType' filter',",
                        Detail = $"'{subType}' is not a valid EntrySubType."
                    });
                }
                query = query.Where(e => e.SubType == parsedSubType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<Domain.Enums.EntryStatus>(status.Trim(), true, out var parsedStatus))
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "invalid 'status' filter',",
                        Detail = $"'{status}' is not a valid EntryStatus."
                    });
                }
                query = query.Where(e => e.Status == parsedStatus);
            }

            if (!string.IsNullOrWhiteSpace(tag))
            {
                var tname = tag.Trim().ToLowerInvariant();
                query = query.Where(e => e.EntryTags.Any(et => et.Tag.Name.ToLower() == tname));
            }

            // Sorting
            var asc = string.Equals(dir, "asc", StringComparison.OrdinalIgnoreCase);
            switch ((sort ?? "updated").Trim().ToLowerInvariant())
            {
                case "title":
                    query = asc ? query.OrderBy(e => e.Title) : query.OrderByDescending(e => e.Title);
                    break;

                case "created":
                    query = asc ? query.OrderBy(e => e.CreatedAtUtc) : query.OrderByDescending(e => e.CreatedAtUtc);
                    break;

                case "rating":
                    // Push null ratings to the end consistently, then sort by rating
                    query = asc
                        ? query.OrderBy(e => e.Rating == null).ThenBy(e => e.Rating)
                        : query.OrderBy(e => e.Rating == null).ThenByDescending(e => e.Rating);
                    break;

                case "updated":
                default:
                    query = asc ? query.OrderBy(e => e.UpdatedAtUtc) : query.OrderByDescending(e => e.UpdatedAtUtc);
                    break;
            }

            // Page + map
            var total = await query.CountAsync(ct);
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var dtos = items.Select(e => e.ToDto()).ToArray();

            return Ok(new PagedResult<MediaEntryDto>(dtos, total, page, pageSize));
        }

        // GET: api/mediaentries/{id}
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(MediaEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();

            var entity = await _db.MediaEntries.AsNoTracking().Include(e => e.EntryTags)!.ThenInclude(et => et.Tag).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

            if (entity is null)
            {
                return NotFound();
            }
            return Ok(entity.ToDto());
        }

        // POST: api/mediaentries
        [HttpPost]
        [ProducesResponseType(typeof(MediaEntryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateMediaEntryDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = GetUserId();

            // Map & validate via mapping helpers
            var (entity, err) = dto.ToEntity(userId);
            if (err is not null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = err,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Persist entry first (auditing timestamps set in SaveChanges)
            await _db.MediaEntries.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);

            // Sync tags from string names -> EntryTags rows
            await _tagSync.SyncAsync(entity, dto.Tags, userId, ct);
            await _db.SaveChangesAsync(ct);

            // Return freshly loaded resource
            var fresh = await _db.MediaEntries.AsNoTracking().Include(e => e.EntryTags)!.ThenInclude(et => et.Tag).FirstAsync(e => e.Id == entity.Id, ct);

            return CreatedAtAction(nameof(GetById), new { id = fresh.Id }, fresh.ToDto());
        }

        // PUT: api/mediaentries/{id}
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(MediaEntryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateMediaEntryDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            // Keep route/body ids consistent if client sends both
            if (dto.Id != Guid.Empty && dto.Id != id)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = "Body Id does not match route id.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var userId = GetUserId();

            // Load tracked entity + tags for reconciliation
            var entity = await _db.MediaEntries.Include(e => e.EntryTags)!.ThenInclude(et => et.Tag).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

            if (entity is null)
            {
                return NotFound();
            }

            // Apply incoming fields (validates enums + cross-field rules)
            var err = dto.ApplyTo(entity);
            if (err is not null)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Validation error",
                    Detail = err,
                    Status = StatusCodes.Status400BadRequest
                });
            }

            // Save scalar changes
            await _db.SaveChangesAsync(ct);

            // Reconcile tag set
            await _tagSync.SyncAsync(entity, dto.Tags, userId, ct);
            await _db.SaveChangesAsync(ct);

            // Return updated shape
            var fresh = await _db.MediaEntries.AsNoTracking().Include(e => e.EntryTags)!.ThenInclude(et => et.Tag).FirstAsync(e => e.Id == id, ct);

            return Ok(fresh.ToDto());
        }

        // DELETE: api/mediaentries/{id}
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = GetUserId();

            var entity = await _db.MediaEntries.Include(e => e.EntryTags).FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);

            if (entity is null)
            {
                return NotFound();
            }

            // Soft-delete happens in AppDbContext.SaveChanges override
            _db.MediaEntries.Remove(entity);
            await _db.SaveChangesAsync(ct);

            return NoContent();
        }

        // ----- helpers -----

        private Guid GetUserId()
        {
            // Reads the NameIdentifier claim (issued this in JwtTokenService)
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(sub))
                throw new UnauthorizedAccessException("Missing NameIdentifier claim.");
            return Guid.Parse(sub);
        }
    }

    // Small paging envelope for list endpoint responses
    public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
}
