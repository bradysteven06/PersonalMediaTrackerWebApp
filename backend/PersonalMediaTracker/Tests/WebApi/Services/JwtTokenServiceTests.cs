// Purpose:
// - Prove JwtTokenService emits a correctly signed JWT that matches our app config:
//   issuer, audience, 1-hour expiry, standard identity claims, and roles.
// - Also prove the token validates with the same parameters used by Program.cs.

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using WebApi.Services;
using Xunit;

namespace Tests.WebApi.Services
{
    public sealed class JwtTokenServiceTests
    {
        private static IConfiguration BuildConfig() =>
            new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string?>("Jwt:Issuer",   "PersonalMediaTracker"),
                    new KeyValuePair<string,string?>("Jwt:Audience", "PersonalMediaTracker.SPA"),
                    // 256-bit (32+ chars) dev key. In production this should come from secrets/KeyVault.
                    new KeyValuePair<string,string?>("Jwt:Key",      "TEST_TEST_TEST_TEST_TEST_TEST_TEST_123456")
                })
                .Build();

        [Fact]
        public void CreateAccessToken_EmitsExpectedClaims_Issuer_Audience_AndExpiry()
        {
            // Arrange
            var cfg = BuildConfig();
            var svc = new JwtTokenService(cfg);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user@example.com"
            };
            var roles = new[] { "Admin", "Editor" };

            // Act
            var jwt = svc.CreateAccessToken(user, roles);

            // Parse without validating to inspect contents
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            // Assert: issuer/audience
            Assert.Equal("PersonalMediaTracker", token.Issuer);
            Assert.Contains("PersonalMediaTracker.SPA", token.Audiences);

            // Assert: standard claims we add in JwtTokenService
            // sub + NameIdentifier should both be user.Id
            var sub = token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value;
            var nameId = token.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value;
            Assert.Equal(user.Id.ToString(), sub);
            Assert.Equal(user.Id.ToString(), nameId);

            // email + name
            Assert.Equal("user@example.com",
                token.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
            Assert.Equal("user@example.com",
                token.Claims.Single(c => c.Type == ClaimTypes.Name).Value);

            // roles appear as multiple ClaimTypes.Role claims
            var roleClaims = token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
            Assert.Contains("Admin", roleClaims);
            Assert.Contains("Editor", roleClaims);

            // Expiration ~ 1 hour from now (JwtTokenService uses DateTime.UtcNow.AddHours(1))
            var nowUtc = DateTime.UtcNow;
            // token.ValidTo is UTC
            Assert.InRange(token.ValidTo, nowUtc.AddMinutes(55), nowUtc.AddMinutes(65));
        }

        [Fact]
        public void Token_Validates_WithAppParameters_AndProducesPrincipal()
        {
            // Arrange
            var cfg = BuildConfig();
            var svc = new JwtTokenService(cfg);

            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user@example.com" };
            var roles = new[] { "Viewer" };
            var jwt = svc.CreateAccessToken(user, roles);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
            var parms = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = cfg["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = cfg["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            // Act
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(jwt, parms, out var validatedToken);

            // Assert: signature & lifetime were valid and claims are available
            Assert.NotNull(validatedToken);
            Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
            Assert.Equal("user@example.com", principal.FindFirstValue(ClaimTypes.Name));
            Assert.Contains(principal.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Viewer");
        }
    }
}
