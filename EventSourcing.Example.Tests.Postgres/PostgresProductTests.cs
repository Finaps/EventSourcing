using Finaps.EventSourcing.Example.Tests;

namespace EventSourcing.Example.Tests.Postgres;

public class PostgresProductTests : ProductTests
{
    public PostgresProductTests() : base(PostgresTestServer.GetServer()) { }
}