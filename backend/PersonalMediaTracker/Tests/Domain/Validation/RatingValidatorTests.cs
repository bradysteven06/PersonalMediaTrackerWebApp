// Tests/Domain/Validation/RatingValidatorTests.cs
// Purpose:
// - Pin the rating contract used by the API and EF model:
//   nullable; 0..10 inclusive; step of 0.5 only.
// Why:
// - EF allows only 1 decimal place (HasPrecision(4,1)) and DTOs have [Range(0,10)].
//   a step rule of (0.5) was added to prevent values like 7.3 from sneaking in.

using FluentAssertions;
using Microsoft.AspNetCore.Components.Forms;
using WebApi.Validation;
using Xunit;

namespace Tests.Domain.Validation
{
    public sealed class RatingValidatorTests
    {
        // -------------------------
        // Happy-path (valid values)
        // -------------------------

        [Theory]
        [InlineData(null)]   // nullable is allowed
        [InlineData(0.0)]    // inclusive lower bound
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(7.5)]
        [InlineData(9.5)]
        [InlineData(10.0)]   // inclusive upper bound
        public void IsValidRating_ValidValues_ReturnsTrue(double? input)
        {
            decimal? rating = input.HasValue ? (decimal)input.Value : (decimal?)null;

            var ok = RatingValidator.IsValid(rating, out var error);
            ok.Should().BeTrue();
            error.Should().BeNull();
        }

        // --------------------------
        // Guard-rails (invalid data)
        // --------------------------

        [Theory]
        [InlineData(-0.5, "between 0 and 10")]      // below range
        [InlineData(10.5, "between 0 and 10")]      // above range
        [InlineData(7.3, "increments of 0.5")]      // wrong step
        [InlineData(0.3, "increments of 0.5")]      // wrong step
        [InlineData(7.55, "one decimal place")]     // would be rejected by precision anyway
        public void IsValidRating_InvalidValues_ReturnsFalse_WithHelpfulMessage(double input, string expectedPhrase)
        {
            decimal rating = (decimal)input;

            var ok = RatingValidator.IsValid(rating, out var error);
            ok.Should().BeFalse();
            error.Should().NotBeNull();
            error!.ToLowerInvariant().Should().Contain(expectedPhrase.ToLowerInvariant());
        }
    }
}
