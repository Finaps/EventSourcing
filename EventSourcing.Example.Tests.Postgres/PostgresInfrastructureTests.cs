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
        var recordStore = PostgresTestServer.Server.Services.GetService<IRecordStore>();
        
        Assert.Equal(typeof(EntityFrameworkRecordStore), recordStore!.GetType());
    }
}