using Finaps.EventSourcing.Example.Tests;

namespace EventSourcing.Example.Tests.Cosmos;

public class CosmosBasketTests : BasketTests
{
    public CosmosBasketTests() : base(CosmosTestServer.GetServer()) { }
}