// Purpose: Unit tests for AuthController covering register/login/me.
// Strategy:
// - Mock Identity's UserManager/SignInManager and the IJwtTokenService.
// - Never hit the DB or real token generator.
// - Verify success + failure branches and returned shapes.
// Notes:
// - Assert on ActionResult payloads (records) directly.
// - Set HttpContext.User for /me using a NameIdentifier claim.

using System.ComponentModel;
using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebApi.Controllers;
using WebApi.Services;
using Xunit;

namespace Tests.WebApi.Controllers
{
    public class AuthControllerTests
    {
        // -----------------------
        // Test plumbing / helpers
        // -----------------------

        // Builds a mock UserManager with only the bits we use configured.
        private static Mock<UserManager<ApplicationUser>> MockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<IOptions<IdentityOptions>>(),                   // optionsAccessor
                Mock.Of<IPasswordHasher<ApplicationUser>>(),            // passwordHasher
                Array.Empty<IUserValidator<ApplicationUser>>(),         // userValidators
                Array.Empty<IPasswordValidator<ApplicationUser>>(),     // passwordValidators
                Mock.Of<ILookupNormalizer>(),                           // keyNormalizer
                new IdentityErrorDescriber(),                           // errors
                Mock.Of<IServiceProvider>(),                            // services
                Mock.Of<ILogger<UserManager<ApplicationUser>>>());      // logger
        }

        // Builds a mock SignInManager. AuthController doesn't call it directly
        // right now, but it's in the constructor, so provide a minimal stub.
        private static Mock<SignInManager<ApplicationUser>> MockSignInManager(UserManager<ApplicationUser> userManager)
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new Mock<SignInManager<ApplicationUser>>(
                userManager,                                            // UserManager
                contextAccessor.Object,                                 // IHttpContextAccessor
                claimsFactory.Object,                                   // IUserClaimsPrincipalFactory
                null!,                                                  // IOptions<IdentityOptions>
                Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),     // ILogger
                null!,                                                  // IAuthenticationSchemeProvider
                null!);                                                 // IUserConfirmation<ApplicationUser>
        }

        // Creates the controller with injected mocks. Optionally injects an
        // HttpContext with a NameIdentifier claim for /me tests.
        private static AuthController CreateController(
            Mock<UserManager<ApplicationUser>> um,
            Mock<SignInManager<ApplicationUser>> sm,
            Mock<IJwtTokenService> jwt,
            Guid? userIdForHttpContext = null)
        {
            var controller = new AuthController(um.Object, sm.Object, jwt.Object);


            var principal = userIdForHttpContext.HasValue
                ? new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userIdForHttpContext.Value.ToString()),
                    new Claim(ClaimTypes.Name, "tester@example.com")
                }, "TestAuth"))
                : new ClaimsPrincipal(new ClaimsIdentity()); // empty identity

                controller.ControllerContext.HttpContext = new DefaultHttpContext
                {
                    User = principal
                };
            

            return controller;
        }

        // --------
        // Register
        // --------

        [Fact]
        public async Task Register_Succeeds_ReturnsToken_And_UserInfo()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user@example.com" };

            var um = MockUserManager();
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), "Passw0rd!"))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<ApplicationUser, string>((u, _) =>
                {
                    // mimic Identity storing the created user object
                    u.Id = user.Id; u.Email = user.Email; u.UserName = user.UserName;
                });

            um.Setup(s => s.GetRolesAsync(It.Is<ApplicationUser>(u => u.Id == user.Id)))
                .ReturnsAsync(new List<string>());

            var sm = MockSignInManager(um.Object);

            var jwt = new Mock<IJwtTokenService>();
            jwt.Setup(j => j.CreateAccessToken(It.IsAny<ApplicationUser>(), It.IsAny<IEnumerable<string>>()))
            .Returns("jwt-token");

            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Register(new AuthController.RegisterDto(user.Email!, "Passw0rd!"));

            // Assert
            var ok = Assert.IsType<ActionResult<AuthController.AuthResponse>>(result);
            var payload = Assert.IsType<AuthController.AuthResponse>(ok.Value);
            Assert.Equal("jwt-token", payload.AccessToken);
            Assert.Equal(user.Email, payload.Email);
            Assert.Equal(user.Id.ToString(), payload.UserId);
        }

        [Fact]
        public async Task Register_Fails_ReturnsBadRequestWithErrors()
        {
            // Arrange
            var um = MockUserManager();
            um.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail", Description = "Email already used." }));

            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();
            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Register(new AuthController.RegisterDto("dupe@example.com", "Passw0rd!"));

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            var errors = Assert.IsAssignableFrom<IEnumerable<IdentityError>>(bad.Value);
            Assert.Contains(errors, e => e.Code == "DuplicateEmail");
        }

        // -------
        // Login
        // -------

        [Fact]
        public async Task Login_Success_ReturnsTokenAndUserInfo()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user@example.com" };

            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync(user);
            um.Setup(x => x.CheckPasswordAsync(user, "Passw0rd!")).ReturnsAsync(true);
            um.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string>());

            var sm = MockSignInManager(um.Object);

            var jwt = new Mock<IJwtTokenService>();
            jwt.Setup(j => j.CreateAccessToken(user, It.IsAny<IEnumerable<string>>()))
                .Returns("jwt-token");

            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Login(new AuthController.LoginDto("user@example.com", "Passw0rd!"));

            // Assert
            var ok = Assert.IsType<ActionResult<AuthController.AuthResponse>>(result);
            var payload = Assert.IsType<AuthController.AuthResponse>(ok.Value);
            Assert.Equal("jwt-token", payload.AccessToken);
            Assert.Equal(user.Email, payload.Email);
            Assert.Equal(user.Id.ToString(), payload.UserId);
        }

        [Fact]
        public async Task Login_UnknownEmail_ReturnsUnauthorized()
        {
            // Arrange
            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync("nope@example.com")).ReturnsAsync((ApplicationUser?)null);

            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();
            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Login(new AuthController.LoginDto("nope@example.com", "whatever"));

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedResult>(result.Result);
            Assert.Equal(StatusCodes.Status401Unauthorized, unauthorized.StatusCode);
        }

        [Fact]
        public async Task Login_WrongPassword_ReturnsUnauthorized()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com" };

            var um = MockUserManager();
            um.Setup(x => x.FindByEmailAsync("user@example.com")).ReturnsAsync(user);
            um.Setup(x => x.CheckPasswordAsync(user, "NOPE")).ReturnsAsync(false);

            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();
            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Login(new AuthController.LoginDto("user@example.com", "NOPE"));

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        // -------
        // Me
        // -------

        [Fact]
        public async Task Me_WithAuthenticatedUser_ReturnsProfile()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "me@example.com", UserName = "me@example.com" };

            var um = MockUserManager();
            um.Setup(x => x.FindByIdAsync(user.Id.ToString())).ReturnsAsync(user);

            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();

            var controller = CreateController(um, sm, jwt, userIdForHttpContext: user.Id);

            // Act
            var result = await controller.Me();

            // Normalize payload from either OkObjectResult or the implicit "Value" path.
            object? payload = (result.Result as OkObjectResult)?.Value ?? result.Value;
            Assert.NotNull(payload);

            // Serialize to JSON so we can assert the shape without dynamic/anonymous types.
            string json = JsonSerializer.Serialize(payload);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Assert 
            Assert.Equal(user.Id.ToString(), root.GetProperty("Id").GetString());
            Assert.Equal(user.Email, root.GetProperty("Email").GetString());
            Assert.Equal(user.UserName, root.GetProperty("UserName").GetString());
        }

        [Fact]
        public async Task Me_NoNameIdentifierClaim_ReturnsUnauthorized()
        {
            // Arrange
            var um = MockUserManager();
            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();
            var controller = CreateController(um, sm, jwt);

            // Act
            var result = await controller.Me();

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task Me_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            var missingId = Guid.NewGuid();

            var um = MockUserManager();
            um.Setup(x => x.FindByIdAsync(missingId.ToString())).ReturnsAsync((ApplicationUser?)null);

            var sm = MockSignInManager(um.Object);
            var jwt = new Mock<IJwtTokenService>();
            var controller = CreateController(um, sm, jwt, userIdForHttpContext: missingId);

            // Act
            var result = await controller.Me();

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }



    }
}
