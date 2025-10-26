// Purpose: end-to-end CRUD flow via in-memory server; validates serialization & tags.
using Tests.Shared.Fixtures;
using Xunit;

namespace Tests.WebApi.Integration
{
    [Collection(WebAppFactoryCollection.Name)]
    public class MediaEntriesIntegrationTests
    {
        private readonly WebAppFactoryFixture _factory;
        public MediaEntriesIntegrationTests(WebAppFactoryFixture factory) => _factory = factory;

        // Add tests later for create/list/get/update/delete and sof delete visibility.
    }
}
