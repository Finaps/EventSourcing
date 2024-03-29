﻿using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Example.Controllers;
using Finaps.EventSourcing.Example.Domain.Products;
using Finaps.EventSourcing.Example.Tests.DTOs;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Finaps.EventSourcing.Example.Tests;

public abstract class ProductTests : TestsBase
{
    private const string ProductName = "TestProduct";
    private const int ProductQuantity = 10;
    protected ProductTests(TestServer server) : base(server) { }
    
    [Fact]
    public async Task Can_Create_Product()
    {
        var create = new CreateProduct(ProductName, ProductQuantity);

        var response = await Client.PostAsync("products", create.AsHttpContent());
        var product = await response.AsDto<ProductDto>();

        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product!.Id);
        Assert.Equal(create.Name, product.Name);
        Assert.Equal(create.Quantity, product.Quantity);
    }
    
    [Fact]
    public async Task Can_Get_Product()
    {
        var productId = await (await Client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        var response = await Client.GetAsync($"products/{productId}");
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
        
        var productId = await (await Client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        await Client.PostAsync($"products/{productId}/addStock", new AddStock(quantityToAdd).AsHttpContent());
        var response = await Client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();

        Assert.Equal(ProductQuantity + quantityToAdd, product!.Quantity);
    }
    
    [Fact]
    public async Task Can_Snapshot_Product()
    {
        var snapshotInterval = new ProductSnapshotFactory().SnapshotInterval;
        
        var productId = await (await Client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        for (var i = 0; i < snapshotInterval; i++)
        {
            await Client.PostAsync($"products/{productId}/addStock", new AddStock(1).AsHttpContent());
        }
        var response = await Client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();

        Assert.Equal(ProductQuantity + snapshotInterval, product!.Quantity);

        var snapshotStore = GetService<IRecordStore>();
        var snapshot = await snapshotStore!
            .GetSnapshots<Product>()
            .Where(x => x.AggregateId == productId)
            .AsAsyncEnumerable()
            .FirstAsync() as ProductSnapshot;
        
        Assert.NotNull(snapshot);
        Assert.Equal(ProductName, snapshot!.Name);
        Assert.Equal(ProductQuantity + snapshotInterval - 1, snapshot.Quantity);
        Assert.Empty(snapshot.Reservations!);
    }
}
