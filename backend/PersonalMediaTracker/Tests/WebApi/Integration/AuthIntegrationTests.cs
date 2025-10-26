// Purpose: end-to-end auth flow via in-memory server (register -> login -> me).
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.WebApi.Integration
{
    [Collection(WebAppFactoryCollection.Name)]
    public class AuthIntegrationTests
    {
        private readonly WebAppFactoryFixture _factory;
        public AuthIntegrationTests(WebAppFactoryFixture factory) => _factory = factory;

        // Add tests later for full auth flow and error cases.
    }
}
