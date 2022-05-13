using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.EF;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventSourcing.Example.Tests.Postgres;

public class PostgresInfrastructureTests
{
    [Fact]
    public void EF_RecordStore_Is_Initialized()
    {
        var recordStore = PostgresTestServer.GetServer().Services.GetService<IRecordStore>();
        
        Assert.True(recordStore is EntityFrameworkRecordStore);
    }
}