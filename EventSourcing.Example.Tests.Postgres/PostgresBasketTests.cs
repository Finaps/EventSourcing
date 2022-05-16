using Finaps.EventSourcing.Example.Tests;

namespace EventSourcing.Example.Tests.Postgres;

public class PostgresBasketTests : BasketTests
{
    public PostgresBasketTests() : base(PostgresTestServer.Server) { }
}