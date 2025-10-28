// Validates the three mapping paths in WebApi.Mapping.MediaEntryMappings:
// - Create DTO -> Entity   (ToEntity)
// - Update DTO -> Entity   (ApplyTo)
// - Entity -> Read DTO     (ToDto)
//
// These tests are pure/unit: no EF Core, no HTTP.
// They give fast, deterministic coverage of the public API contract.

using System;
using System.Linq;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebApi.Contracts;
using WebApi.Mapping;
using Xunit;

namespace Tests.Domain.Entities
{
    public class MediaEntryEntityTests
    {
        // -------------------------------
        // Create DTO -> Entity (ToEntity)
        // -------------------------------

        [Fact]
        public void ToEntity_ValidInput_ReturnsEntityWithoutError()
        {
            // Arrange: a well formed create DTO
            var userId = Guid.NewGuid();
            var dto = new CreateMediaEntryDto
            {
                Title = " My Movie ",   // surrounding spaces should be trimmed
                Type = EntryType.Movie,
                SubType = EntrySubType.LiveAction,
                Status = EntryStatus.Planning,
                Rating = 7.5m,
                Notes = " watch later ",
                Tags = new[] { "Action", "Drama" } // tags handled by TagSyncService. not mapped here
            };

            // Act: map to entity
            var (entity, error) = dto.ToEntity(userId);

            // Assert: entity is populated, title/notes trimmed, no error
            error.Should().BeNull();
            entity.Should().NotBeNull();
            entity.UserId.Should().Be(userId);
            entity.Title.Should().Be("My Movie");
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Planning);
            entity.Rating.Should().Be(7.5m);
            entity.Notes.Should().Be("watch later");

            // Tags are NOT set in mapper (TagSync handles joins)
            entity.EntryTags.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void ToEntity_MissingOrWhitespaceTitle_ReturnsError(string? badTitle)
        {
            // Arrange
            var dto = new CreateMediaEntryDto { Title = badTitle ?? string.Empty, Type = EntryType.Series, Status = EntryStatus.Planning };

            // Act
            var (_, error) = dto.ToEntity(Guid.NewGuid());

            // Assert
            error.Should().NotBeNullOrWhiteSpace();
        }

        // ------------------------------
        // Update DTO -> Entity (ApplyTo)
        // ------------------------------

        [Fact]
        public void ApplyTo_WithAllFields_UpdatesEntity()
        {
            // Arrange: start from an existing entity with some values
            var entity = new MediaEntry
            {
                Id      = Guid.NewGuid(),
                UserId  = Guid.NewGuid(),
                Title   = "Old",
                Type    = EntryType.Series,
                SubType = EntrySubType.Anime,
                Status  = EntryStatus.Watching,
                Rating  = 6.0m,
                Notes   = "old notes"
            };

            var dto = new UpdateMediaEntryDto
            {
                // Id may be empty or same as route. mapper ignores it and only applies fields
                Title   = " New Title ",
                Type    = EntryType.Movie,
                SubType = EntrySubType.LiveAction,
                Status  = EntryStatus.Completed,
                Rating  = 9.0m,
                Notes   = " finished "
            };

            // Act:
            var error = dto.ApplyTo(entity);

            // Assert
            error.Should().BeNull();
            entity.Title.Should().Be("New Title");                  // trimmed
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Completed);
            entity.Rating.Should().Be(9.0m);
            entity.Notes.Should().Be("finished");                   // trimmed
        }

        [Fact]
        public void ApplyTo_TitleProvidedButWhitespace_ReturnsErrorAndDoesNotChangeEntity()
        {
            // Arrange
            var original = new MediaEntry
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "KeepMe",
                Type = EntryType.Series,
                Status = EntryStatus.Watching,
            };

            var dto = new UpdateMediaEntryDto { Title = "   " };

            // Act
            var error = dto.ApplyTo(original);

            // Assert
            error.Should().NotBeNullOrWhiteSpace();
            original.Title.Should().Be("KeepMe");   // unchanged
        }

        [Fact]
        public void ApplyTo_NullFields_DoNotChangeEntity()
        {
            // Arrange
            var entity = new MediaEntry
            {
                Title   = "Existing",
                Type    = EntryType.Movie,
                SubType = EntrySubType.LiveAction,
                Status  = EntryStatus.Completed,
                Rating  = 8.0m,
                Notes   = "Done"
            };

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
            var error = dto.ApplyTo(entity);

            // Assert
            error.Should().BeNull();
            entity.Title.Should().Be("Existing");
            entity.Type.Should().Be(EntryType.Movie);
            entity.SubType.Should().Be(EntrySubType.LiveAction);
            entity.Status.Should().Be(EntryStatus.Completed);
            entity.Rating.Should().Be(8.0m);
            entity.Notes.Should().Be("Done");
        }

        // --------------------------
        // Entity -> Read DTO (ToDto)
        // --------------------------

        [Fact]
        public void ToDto_MapsAllFieldsAndMaterializesTags()
        {
            // Arrange: build an entity with two tags via explicit join rows
            var entry = new MediaEntry
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Title = "Tagged Show",
                Type = EntryType.Series,
                SubType = EntrySubType.Animated,
                Status = EntryStatus.Watching,
                Rating = 7.0m,
                Notes = "notes here",
                EntryTags =
                {
                    new EntryTag { Tag = new Tag { Name = "action" } },
                    new EntryTag { Tag = new Tag { Name = "drama" } }
                }
            };

            // Act
            var dto = entry.ToDto();

            // Assert: scalar fields copied. tags projected to string list
            dto.Id.Should().Be(entry.Id);
            dto.UserId.Should().Be(entry.UserId);
            dto.Title.Should().Be("Tagged Show");
            dto.Type.Should().Be(EntryType.Series);
            dto.SubType.Should().Be(EntrySubType.Animated);
            dto.Status.Should().Be(EntryStatus.Watching);
            dto.Rating.Should().Be(7.0m);
            dto.Notes.Should().Be("notes here");

            // Order of tags depends on EntryTags order. assert as a set
            dto.Tags.Should().BeEquivalentTo(new[] { "action", "drama" });
        }
    }
}
