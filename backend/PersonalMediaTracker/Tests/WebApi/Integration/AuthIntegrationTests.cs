// End-to-end tests for AuthController using the in-memory Web API host
// (WebAppFactoryFixture). These validate the whole pipeline:
// - Register -> Login -> Me happy path
// - Duplicate registration returns 400
// - Wrong password returns 401

using System;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.WebApi.Integration
{
    [Collection(WebAppFactoryCollection.Name)]
    public class AuthIntegrationTests
    {
        private readonly WebAppFactoryFixture _factory;
        public AuthIntegrationTests(WebAppFactoryFixture factory) => _factory = factory;

        [Fact]
        public async Task Register_Login_Me_HappyPath()
        {
            // Arrange
            var client = _factory.CreateClientPlain();
            var email = $"user_{Guid.NewGuid():N}@example.com";
            var password = "Passw0rd!";

            // Register
            var reg = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

            // Login
            var login = await client.PostAsJsonAsync("/api/auth/login", new{ email, password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);

            // Parse token
            var loginJson = await login.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(loginJson);
            var token = doc.RootElement.GetProperty("accessToken").GetString();
            Assert.False(string.IsNullOrWhiteSpace(token));

            // Authenticated call to /api/auth/me
            var authed = await _factory.CreateAuthenticatedClientAsync(email, password);
            var me = await authed.GetAsync("/api/auth/me");
            Assert.Equal(HttpStatusCode.OK, me.StatusCode);

            var meObj = await me.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(email, meObj.GetProperty("email").GetString());
        }

        [Fact]
        public async Task Register_DuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var client = _factory.CreateClientPlain();
            var email = $"dupe_{Guid.NewGuid():N}@example.com";
            var password = "Passw0rd!";

            // First registration should succeed
            var reg1 = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.OK, reg1.StatusCode);

            // Act: second registration with same email
            var reg2 = await client.PostAsJsonAsync("/api/auth/register", new { email, password });

            // Assert: Identity returns errors -> 400 BadRequest
            Assert.Equal(HttpStatusCode.BadRequest, reg2.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            // Arrange
            var client = _factory.CreateClientPlain();
            var email = $"wrongpw_{Guid.NewGuid():N}@example.com";
            var password = "Passw0rd!";

            // Create user
            var reg = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
            Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

            // Act: wrong password
            var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "NOPE!" });

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        }
    }
}
