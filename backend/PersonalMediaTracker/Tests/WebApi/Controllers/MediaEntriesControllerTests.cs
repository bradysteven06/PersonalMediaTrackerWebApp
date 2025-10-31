// Purpose: Unit tests for MediaEntriesController that do NOT hit real EF/SQL database.
// Strategy:
// - Use EF Core InMemory for a transient DbContext so seed/query can be done quickly.
// - Mock TagSyncService with Moq to assert it's called when expected.
// - Inject a fake authenticated User (NameIdentifier claim) so GetUserId() works.
// - Cover key controller behaviors: validation, not-found, happy paths, and soft-delete intent.
//
// Notes:
// - These tests are intentionally "controller-only".EF model details are not being verified.
//   (Those are covered by the TagSync + integration tests.)
// - Keep each test focused. Multiple assertions are fine for the single behavior being tested.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using WebApi.Contracts;
using WebApi.Controllers;
using WebApi.Services;
using Xunit;

namespace Tests.WebApi.Controllers
{
    public class MediaEntriesControllerTests
    {
        // -----------------------
        // Helpers / Test plumbing
        // -----------------------

        // Creates a new AppDbContext backed by EF InMemory for this test instance.
        private static AppDbContext CreateInMemoryDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            return new AppDbContext(opts);
        }

        // Builds a controller with the provided DbContext + mocked TagSyncService,
        // and injects an HttpContext with a fake authenticated user.
        private static MediaEntriesController CreateController(
            AppDbContext db,
            Mock<ITagSyncService> tagSyncMock,
            Guid? userId = null)
        {
            var controller = new MediaEntriesController(db, tagSyncMock.Object);

            var uid = userId ?? Guid.NewGuid();
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, uid.ToString()),
                new Claim(ClaimTypes.Name, "tester@example.com")
            }, "TestAuth");

            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };

            return controller;
        }

        // Seed a simple MediaEntry for a user (optionally with tags)
        private static MediaEntry SeedEntry(AppDbContext db, Guid userId, string title = "Seeded", EntryType type = EntryType.Series)
        {
            var entry = new MediaEntry
            {
                UserId  = userId,
                Title   = title,
                Type    = type,
                Status  = EntryStatus.Planning
            };
            db.MediaEntries.Add(entry);
            db.SaveChanges();
            return entry;
        }

        // -------------
        // List endpoint
        // -------------

        [Fact]
        public async Task List_InvalidTypeFilter_ReturnsBadRequest()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict); // constructor requires AppDbContext
            var controller = CreateController(db, tagSync);

            var result = await controller.List(q: null, type: "NOT_A_VALID_ENUM", subType: null, status: null,
                                               tag: null, sort: "updated", dir: "desc", page: 1, pageSize: 20, ct: default);

            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ProblemDetails>(bad.Value);
        }

        [Fact]
        public async Task List_Defaults_ReturnsOkWithEnvelope()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var userId = Guid.NewGuid();

            // seed two entries for this user and one for a different user (should be filtered)
            SeedEntry(db, userId, "A");
            SeedEntry(db, userId, "B");
            SeedEntry(db, Guid.NewGuid(), "OtherUser");

            var controller = CreateController(db, tagSync, userId);

            var result = await controller.List(null, null, null, null, null, "updated", "desc", 1, 20, default);
            var ok = Assert.IsType<OkObjectResult>(result);
            var envelope = Assert.IsType<PagedResult<MediaEntryDto>>(ok.Value);

            Assert.True(envelope.Total >= 2);
            Assert.All(envelope.Items, dto => Assert.Equal(userId, dto.UserId));
        }

        // ----------------
        // GetById endpoint
        // ----------------

        [Fact]
        public async Task GetById_NotFound_Returns404()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var controller = CreateController(db, tagSync);

            var result = await controller.GetById(Guid.NewGuid(), default);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GetById_Found_ReturnsOkWithDto()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var userId = Guid.NewGuid();
            var seeded = SeedEntry(db, userId, "FoundMe");

            var controller = CreateController(db, tagSync, userId);

            var result = await controller.GetById(seeded.Id, default);
            var ok = Assert.IsType<OkObjectResult>(result);
            var dto = Assert.IsType<MediaEntryDto>(ok.Value);
            Assert.Equal("FoundMe", dto.Title);
        }

        // -------------
        // Create (Post)
        // -------------

        [Fact]
        public async Task Create_InvalidModelState_ReturnsValidationProblem()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var controller = CreateController(db, tagSync);

            // Force model state invalid (simulate data annotations failure)
            controller.ModelState.AddModelError("Title", "Required");

            var dto = new CreateMediaEntryDto { Title = "", Type = EntryType.Movie, Status = EntryStatus.Planning };
            var result = await controller.Create(dto, default);

            var bad = Assert.IsType<ObjectResult>(result);
            var details = Assert.IsType<ValidationProblemDetails>(bad.Value);
            Assert.Contains("Title", details.Errors.Keys);
        }

        [Fact]
        public async Task Create_Valid_CallsTagSync_AndReturnsCreated()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>();
            tagSync
                .Setup(s => s.SyncAsync(It.IsAny<MediaEntry>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var controller = CreateController(db, tagSync);

            var dto = new CreateMediaEntryDto
            {
                Title   = "New Movie",
                Type    = EntryType.Movie,
                Status  = EntryStatus.Planning,
                Tags    = new[] { "Action", "Drama" }
            };

            var result = await controller.Create(dto, default);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(MediaEntriesController.GetById), created.ActionName);
            var payload = Assert.IsType<MediaEntryDto>(created.Value);
            Assert.Equal("New Movie", payload.Title);

            tagSync.Verify(s => s.SyncAsync(
                It.Is<MediaEntry>(e => e.Title == "New Movie"),
                It.Is<IEnumerable<string>>(t => t.Contains("Action") && t.Contains("Drama")),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        // ------------
        // Update (PUT)
        // ------------

        [Fact]
        public async Task Update_MismatchedIds_ReturnsBadRequest()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var controller = CreateController(db, tagSync);

            var routeId = Guid.NewGuid();
            var body = new UpdateMediaEntryDto { Id = Guid.NewGuid(), Title = "X" };

            var result = await controller.Update(routeId, body, default);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var problem = Assert.IsType<ProblemDetails>(bad.Value);
            Assert.Equal("Validation error", problem.Title);
        }

        [Fact]
        public async Task Update_NotFound_Returns404()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var controller = CreateController(db, tagSync);

            var result = await controller.Update(Guid.NewGuid(), new UpdateMediaEntryDto { Title = "Nope" }, default);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Update_Valid_UpdatesEntity_CallsTagSync_ReturnsOk()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>();
            tagSync
                .Setup(s => s.SyncAsync(It.IsAny<MediaEntry>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var userId = Guid.NewGuid();
            var seeded = SeedEntry(db, userId, "Before", EntryType.Series);

            var controller = CreateController(db, tagSync, userId);

            var dto = new UpdateMediaEntryDto
            {
                Id      = seeded.Id,
                Title   = "After",
                Status  = EntryStatus.Watching,
                Tags    = new[] { "SciFi" }
            };

            var result = await controller.Update(seeded.Id, dto, default);

            var ok = Assert.IsType<OkObjectResult>(result);
            var returned = Assert.IsType<MediaEntryDto>(ok.Value);
            Assert.Equal("After", returned.Title);
            Assert.Equal(EntryStatus.Watching, returned.Status);

            tagSync.Verify(s => s.SyncAsync(
                It.Is<MediaEntry>(e => e.Id == seeded.Id && e.Title == "After"),
                It.Is<IEnumerable<string>>(tags => tags.Single() == "SciFi"),
                It.Is<Guid>(g => g == userId),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_TitleWhitespace_ReturnsBadRequest()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var userId = Guid.NewGuid();
            var seeded = SeedEntry(db, userId, "Keep");

            var controller = CreateController(db, tagSync, userId);
            var dto = new UpdateMediaEntryDto { Id = seeded.Id, Title = "   " };

            var result = await controller.Update(seeded.Id, dto, default);
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.IsType<ProblemDetails>(bad.Value);
        }

        // ------------
        // Delete (DEL)
        // ------------

        [Fact]
        public async Task Delete_NotFound_Returns404()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var controller = CreateController(db, tagSync);

            var result = await controller.Delete(Guid.NewGuid(), default);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Found_ReturnsNoContent()
        {
            using var db = CreateInMemoryDb();
            var tagSync = new Mock<ITagSyncService>(MockBehavior.Strict);
            var userId = Guid.NewGuid();
            var seeded = SeedEntry(db, userId, "Gone");

            var controller = CreateController(db, tagSync, userId);

            var result = await controller.Delete(seeded.Id, default);
            Assert.IsType<NoContentResult>(result);

            // In AppDbContext.SaveChanges() a hard delete becomes a soft-delete,
            // but with InMemory provider we can at least assert it no longer exists in the set.
            Assert.False(db.MediaEntries.Any(e => e.Id == seeded.Id));
        }
    }
}
