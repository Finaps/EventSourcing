using EventSourcing.Core;
using EventSourcing.Cosmos;
using EventSourcing.Example.Domain.Products;
using EventSourcing.Example.Tests.DTOs;
using Xunit;

namespace EventSourcing.Example.Tests;

public class ProductTests : TestsBase
{
    private const string ProductName = "TestProduct";
    private const int ProductQuantity = 10;
    private readonly HttpClient _client = Server.CreateClient();

    [Fact]
    public async Task Can_Create_Product()
    {
        var create = new CreateProduct(ProductName, ProductQuantity);

        var response = await _client.PostAsync("products", create.AsHttpContent());
        var product = await response.AsDto<ProductDto>();

        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product!.Id);
        Assert.Equal(create.Name, product.Name);
        Assert.Equal(create.Quantity, product.Quantity);
    }
    
    [Fact]
    public async Task Can_Get_Product()
    {
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        var response = await _client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();

        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product!.Id);
        Assert.Equal(ProductName, product.Name);
        Assert.Equal(ProductQuantity, product.Quantity);
    }
    
    [Fact]
    public async Task Can_Add_Stock()
    {
        const int quantityToAdd = 5;
        
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        await _client.PostAsync($"products/{productId}/addStock", new AddStock(quantityToAdd).AsHttpContent());
        var response = await _client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();

        Assert.Equal(ProductQuantity + quantityToAdd, product!.Quantity);
    }
    
    [Fact]
    public async Task Can_Snapshot_Product()
    {
        var snapshotInterval = new ProductSnapshotFactory().SnapshotInterval;
        
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        for (var i = 0; i < snapshotInterval; i++)
        {
            await _client.PostAsync($"products/{productId}/addStock", new AddStock(1).AsHttpContent());
        }
        var response = await _client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();

        Assert.Equal(ProductQuantity + snapshotInterval, product!.Quantity);

        var snapshotStore = GetService<IRecordStore>();
        var snapshot = await snapshotStore!.Snapshots.Where(x => x.AggregateId == productId).AsAsyncEnumerable().FirstAsync() as ProductSnapshot;
        
        Assert.NotNull(snapshot);
        Assert.Equal(ProductName, snapshot!.Name);
        Assert.Equal(ProductQuantity + snapshotInterval - 1, snapshot.Quantity);
        Assert.Empty(snapshot.Reservations);
    }
    
    [Fact]
    public async Task Can_Execute_Stored_Procedure()
    {
        var create = new CreateProduct(ProductName, ProductQuantity);
        var response = await _client.PostAsync("products", create.AsHttpContent());
        var product = await response.AsDto<ProductDto>();
        await _client.PostAsync($"products/{product!.Id}/addStock", new AddStock(1).AsHttpContent());
        
        var eventStore = GetService<IRecordStore>();
        var cosmosStore = eventStore as CosmosRecordStore;
        var result = await cosmosStore!.DeleteEvents(Guid.Empty, product.Id);
        Assert.Equal(1, result);
    }
}
