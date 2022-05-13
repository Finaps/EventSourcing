using Finaps.EventSourcing.Example.Tests;

namespace EventSourcing.Example.Tests.Cosmos;

public class CosmosProductTests : ProductTests
{
    public CosmosProductTests() : base(CosmosTestServer.GetServer()) { }
}