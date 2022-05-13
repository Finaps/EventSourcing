using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventSourcing.Example.Tests.Cosmos;

public class CosmosInfrastructureTests
{
    [Fact]
    public void Cosmos_RecordStore_Is_Initialized()
    {
        var recordStore = CosmosTestServer.GetServer().Services.GetService<IRecordStore>();
        
        Assert.Equal(typeof(CosmosRecordStore), recordStore!.GetType());
    }
}