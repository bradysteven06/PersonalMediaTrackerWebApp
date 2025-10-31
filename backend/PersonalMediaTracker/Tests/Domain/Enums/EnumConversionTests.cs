// Purpose:
// - Pin the public string representations of enums used acros DB, API, and UI.
// - Verify System.Text.Json behavior matches Program.cs configuration (JsonStringEnumCOnverter).
// - Verify case insensitive parsing semantics used in MediaEntriesController filters.
// - Guard against names exceeding EF Core column max length (16) configured in AppDbContext.

using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Enums
{
    public class EnumConversionTests
    {
        // Shared JSON options to mirror Program.cs
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        // --------------------------
        // Contract: exact enum names
        // --------------------------

        [Fact]
        public void EntryType_Names_AreStable()
        {
            // If this fails, you changed an enum name and must review DB mapping + API contract.
            Enum.GetNames(typeof(EntryType))
                .Should()
                .BeEquivalentTo(new[] { "Movie", "Series" }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void EntrySubType_Names_AreStable()
        {
            // If this fails, you changed an enum name and must review DB mapping + API contract.
            Enum.GetNames(typeof(EntrySubType))
                .Should()
                .BeEquivalentTo(new[] { "LiveAction", "Anime", "Manga", "Animated", "Documentary", "Other" }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void EntryStatus_Names_AreStable()
        {
            // If this fails, you changed an enum name and must review DB mapping + API contract.
            Enum.GetNames(typeof(EntryStatus))
                .Should()
                .BeEquivalentTo(new[] { "Planning", "Watching", "Completed", "OnHold", "Dropped" }, options => options.WithStrictOrdering());
        }

        // ------------------------------------------------------
        // JSON: Enum <-> string round trips using API serializer
        // ------------------------------------------------------

        [Theory]
        [InlineData(EntryType.Movie,    "\"Movie\"")]
        [InlineData(EntryType.Series,   "\"Series\"")]
        public void Json_RoundTrip_EntryType(EntryType value, string expectedJson)
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            json.Should().Be(expectedJson);

            var roundTrip = JsonSerializer.Deserialize<EntryType>(json, JsonOpts);
            roundTrip.Should().Be(value);
        }

        [Theory]
        [InlineData(EntrySubType.LiveAction,    "\"LiveAction\"")]
        [InlineData(EntrySubType.Anime,         "\"Anime\"")]
        [InlineData(EntrySubType.Manga,         "\"Manga\"")]
        [InlineData(EntrySubType.Animated,      "\"Animated\"")]
        [InlineData(EntrySubType.Documentary,   "\"Documentary\"")]
        [InlineData(EntrySubType.Other,         "\"Other\"")]
        public void Json_RoundTrip_EntrySubType(EntrySubType value, string expectedJson)
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            json.Should().Be(expectedJson);

            var roundTrip = JsonSerializer.Deserialize<EntrySubType>(json, JsonOpts);
            roundTrip.Should().Be(value);
        }

        [Theory]
        [InlineData(EntryStatus.Planning, "\"Planning\"")]
        [InlineData(EntryStatus.Watching, "\"Watching\"")]
        [InlineData(EntryStatus.Completed, "\"Completed\"")]
        [InlineData(EntryStatus.OnHold, "\"OnHold\"")]
        [InlineData(EntryStatus.Dropped, "\"Dropped\"")]
        public void Json_RoundTrip_EntryStatus(EntryStatus value, string expectedJson)
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            json.Should().Be(expectedJson);

            var roundTrip = JsonSerializer.Deserialize<EntryStatus>(json, JsonOpts);
            roundTrip.Should().Be(value);
        }

        // -----------------------------------------------------------------
        // Parsing semantics: controller uses Enum.TryParse(..., ignoreCase)
        // -----------------------------------------------------------------

        [Theory]
        [InlineData("movie", EntryType.Movie)]
        [InlineData("SERIES", EntryType.Series)]
        public void Parse_EtryType_IsCaseInsensitive(string input, EntryType expected)
        {
            var ok = Enum.TryParse<EntryType>(input, ignoreCase: true, out var actual);
            ok.Should().BeTrue();
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData("planning", EntryStatus.Planning)]
        [InlineData("WATCHING", EntryStatus.Watching)]
        [InlineData("completed", EntryStatus.Completed)]
        public void Parse_EntryStatus_IsCaseInsensitive(string input, EntryStatus expected)
        {
            var ok = Enum.TryParse<EntryStatus>(input, ignoreCase: true, out var actual);
            ok.Should().BeTrue();
            actual.Should().Be(expected);
        }

        [Theory]
        [InlineData("mov1e")]       // not a valid value
        [InlineData("someStatus")]  // not a valid value
        public void Parse_InvalidValues_ReturnFalse(string input)
        {
            Enum.TryParse<EntryType>(input, true, out _).Should().BeFalse();
            Enum.TryParse<EntryStatus>(input, true, out _).Should().BeFalse();
            Enum.TryParse<EntrySubType>(input, true, out _).Should().BeFalse();
        }

        // ----------------------------------------------------------------
        // EF guard: all enum Names must fit the configured max length (16)
        // -----------------------------------------------------------------

        [Fact]
        public void Enum_Names_DoNotExceed_MaxLength16()
        {
            const int MaxLen = 16;

            string? tooLong = 
                Enum.GetNames<EntryType>().Concat(
                Enum.GetNames<EntrySubType>()).Concat(
                Enum.GetNames<EntryStatus>())
                .FirstOrDefault(n => n.Length > MaxLen);

            tooLong.Should().BeNull("Ef mapping uses HasMaxLength(16) for enum columns");
        }
    }
}
