// Tests/Domain/Mappings/MediaEntryMappingsTests.cs
// Purpose:
// - Verify DTO <-> Entity mapping logic in WebApi.Mapping.MediaEntryMappings.
// - These are pure unit tests (no EF/DB), focused on mapping rules & edge cases.
// - Ensures: tuple-based creation mapping, safe ApplyTo updates, normalization rules,
//   and that ToDto materializes tag names from EntryTags as-is.
//
// Why:
// - Mapping bugs are easy to introduce and hard to detect via controller/integration tests.
//   Pinning the contract here keeps controllers thin and reliable.
//
// Covers:
// 1) Create DTO -> Entity (ToEntity)
//    - Title required/trimmed; userId assigned; scalar fields copied; notes normalized.
//    - Tag joins are NOT created here (TagSyncService handles DB reconciliation).
// 2) Update DTO -> Entity (ApplyTo)
//    - Applies only provided fields; rejects whitespace Title; trims strings; normalizes notes.
// 3) Entity -> Read DTO (ToDto)
//    - Copies scalars; materializes Tag names as provided on EntryTags (no re-normalization).
//
// NOTE: If you later change normalization behavior (e.g., lowercasing tags in ToDto),
//       update these tests to reflect the new contract.

using System;
using System.Collections.Generic;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using WebApi.Contracts;
using WebApi.Mapping;
using Xunit;

namespace Tests.Domain.Mappings
{
    public sealed class MediaEntryMappingsTests
    {
        // ---------------------------------------------
        // Helpers
        // ---------------------------------------------

        private static MediaEntry MakeEntity(Guid userId, string title = "Sample")
            => new MediaEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Type = EntryType.Series,
                SubType = EntrySubType.LiveAction,
                Status = EntryStatus.Planning,
                Rating = 8.5m,
                Notes = "hello"
            };

        private static MediaEntry MakeEntityWithTags(Guid userId, IEnumerable<string> tagNames)
        {
            var e = MakeEntity(userId);
            e.EntryTags = tagNames.Select(n => new EntryTag
            {
                MediaEntry = e,
                Tag = new Tag { Id = Guid.NewGuid(), UserId = userId, Name = n }
            }).ToList();
            return e;
        }

        // ---------------------------------------------
        // 1) Create DTO -> Entity (ToEntity)
        // ---------------------------------------------

        [Fact]
        public void ToEntity_ValidInput_ReturnsEntity_NoError_AndTrimsStrings()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var dto = new CreateMediaEntryDto
            {
                Title = "  Blade Runner  ",
                Type = EntryType.Movie,
                SubType = EntrySubType.LiveAction,
                Status = EntryStatus.Planning,
                Rating = 9.0m,
                Notes = "  neo-noir "
                // Tags are intentionally ignored here (handled by TagSyncService)
            };

            // Act
            var (entity, error) = dto.ToEntity(userId);

            // Assert
            error.Should().BeNull();
            entity.Should().NotBeNull();
            entity.Id.Should().NotBeEmpty();
            entity.UserId.Should().Be(userId);

            entity.Title.Should().Be("Blade Runner");     // trimmed
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Planning);
            entity.Rating.Should().Be(9.0m);
            entity.Notes.Should().Be("neo-noir");         // trimmed

            // Mapping must not pre-populate joins; TagSyncService does that.
            entity.EntryTags.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToEntity_MissingOrWhitespaceTitle_ReturnsError(string? bad)
        {
            // Arrange
            var dto = new CreateMediaEntryDto
            {
                Title = bad ?? string.Empty,
                Type = EntryType.Series,
                Status = EntryStatus.Planning
            };

            // Act
            var (_, error) = dto.ToEntity(Guid.NewGuid());

            // Assert
            error.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ToEntity_NotesWhitespace_NormalizesToNull()
        {
            // Arrange
            var dto = new CreateMediaEntryDto
            {
                Title = "X",
                Type = EntryType.Series,
                Status = EntryStatus.Planning,
                Notes = "   " // user cleared notes
            };

            // Act
            var (entity, error) = dto.ToEntity(Guid.NewGuid());

            // Assert
            error.Should().BeNull();
            entity.Notes.Should().BeNull();
        }

        // ---------------------------------------------
        // 2) Update DTO -> Entity (ApplyTo)
        // ---------------------------------------------

        [Fact]
        public void ApplyTo_AllFields_Updates_AndTrims_WithoutAlteringIdentity()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var entity = MakeEntity(userId, "Old");
            entity.Type = EntryType.Series;
            entity.SubType = EntrySubType.Anime;
            entity.Status = EntryStatus.Watching;
            entity.Rating = 6.0m;
            entity.Notes = "old notes";

            var dto = new UpdateMediaEntryDto
            {
                Id = entity.Id,              // mapper ignores Id; controller enforces route/body
                Title = "  New Title  ",
                Type = EntryType.Movie,
                SubType = EntrySubType.LiveAction,
                Status = EntryStatus.Completed,
                Rating = 9.0m,
                Notes = "  finished "
                // Tags are handled outside mapping (TagSyncService)
            };

            // Act
            var err = dto.ApplyTo(entity);

            // Assert
            err.Should().BeNull();

            // Identity untouched
            entity.Id.Should().NotBeEmpty();
            entity.UserId.Should().Be(userId);

            // Fields updated/trimmed
            entity.Title.Should().Be("New Title");
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Completed);
            entity.Rating.Should().Be(9.0m);
            entity.Notes.Should().Be("finished");
        }

        [Fact]
        public void ApplyTo_WhitespaceTitle_ReturnsError_AndDoesNotChangeTitle()
        {
            // Arrange
            var entity = MakeEntity(Guid.NewGuid(), "KeepMe");
            var dto = new UpdateMediaEntryDto { Title = "   " };

            // Act
            var err = dto.ApplyTo(entity);

            // Assert
            err.Should().NotBeNullOrWhiteSpace();
            entity.Title.Should().Be("KeepMe"); // unchanged
        }

        [Fact]
        public void ApplyTo_NullFields_DoNotOverwriteExistingValues()
        {
            // Arrange
            var entity = MakeEntity(Guid.NewGuid(), "Existing");
            entity.Type = EntryType.Movie;
            entity.SubType = EntrySubType.LiveAction;
            entity.Status = EntryStatus.Completed;
            entity.Rating = 8.0m;
            entity.Notes = "Done";

            var dto = new UpdateMediaEntryDto
            {
                Title = null,
                Type = null,
                SubType = null,
                Status = null,
                Rating = null,
                Notes = null
            };

            // Act
            var err = dto.ApplyTo(entity);

            // Assert
            err.Should().BeNull();
            entity.Title.Should().Be("Existing");
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Completed);
            entity.Rating.Should().Be(8.0m);
            entity.Notes.Should().Be("Done");
        }

        [Fact]
        public void ApplyTo_NotesWhitespace_NormalizesToNull()
        {
            // Arrange
            var entity = MakeEntity(Guid.NewGuid());
            entity.Notes = "something";
            var dto = new UpdateMediaEntryDto { Notes = "   " };

            // Act
            var err = dto.ApplyTo(entity);

            // Assert
            err.Should().BeNull();
            entity.Notes.Should().BeNull();
        }

        // ---------------------------------------------
        // 3) Entity -> Read DTO (ToDto)
        // ---------------------------------------------

        [Fact]
        public void ToDto_MapsScalars_And_MaterializesTagNames_AsIs()
        {
            // Arrange: use mixed case tag names to confirm ToDto does NOT re-normalize
            var userId = Guid.NewGuid();
            var entity = MakeEntityWithTags(userId, new[] { "SciFi", "drama", "Thriller" });

            // Act
            var dto = entity.ToDto();

            // Assert: scalars
            dto.Id.Should().Be(entity.Id);
            dto.UserId.Should().Be(userId);
            dto.Title.Should().Be(entity.Title);
            dto.Type.Should().Be(entity.Type);
            dto.SubType.Should().Be(entity.SubType);
            dto.Status.Should().Be(entity.Status);
            dto.Rating.Should().Be(entity.Rating);
            dto.Notes.Should().Be(entity.Notes);

            // Assert: tag names are whatever is on Tag.Name (no additional normalization in ToDto)
            dto.Tags.Should().BeEquivalentTo(new[] { "SciFi", "drama", "Thriller" });
        }
    }
}
