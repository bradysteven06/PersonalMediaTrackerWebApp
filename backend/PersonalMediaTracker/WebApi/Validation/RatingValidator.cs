// Purpose:
// - Centralize rating rules so controllers/mappers can call one place.
// Contract:
// - null is allowed (no rating).
// - 0 <= rating <= 10.
// - Only increments of 0.5 are allowed (0, 0.5, 1.0, ... 10.0).


namespace WebApi.Validation
{
    public static class RatingValidator
    {
        public static bool IsValid(decimal? rating, out string? error)
        {
            error = null;

            if (rating is null) return true; // nullable by design

            var value = rating.Value;

            // Range check (matches DTO [Range(0,10)])
            if (value < 0m || value > 10m)
            {
                error = "Rating must be between 0 and 10 (inclusive).";
                return false;
            }

            // Optional precision hint (helps users before DB precision failure)
            // If more than one decimal place is provided, surface a friendlier error.
            if (HasMoreThanOneDecimalPlace(value))
            {
                error = "Rating must use at most one decimal place (e.g., 7, 7.5, 8.0).";
                return false;
            }

            // Step check: enforce 0.5 increments
            // e.g., 7.5 * 2 = 15 (integer), 7.3 * 2 = 14.6 (not integer)
            var doubled = value * 2m;
            if (doubled != decimal.Truncate(doubled))
            {
                error = "Rating must be in increments of 0.5.";
                return false;
            }

            return true;
        }

        // Helper ensures at most one decimal place without string allocations.
        private static bool HasMoreThanOneDecimalPlace(decimal value)
        {
            // Normalize to one decimal place and compare.
            var scaled = decimal.Truncate(value * 10m) / 10m;
            return scaled != value;
        }
    }
}