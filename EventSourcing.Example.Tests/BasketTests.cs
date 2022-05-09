using System.Net;
using Finaps.EventSourcing.Example.Controllers;
using Finaps.EventSourcing.Example.Tests.DTOs;
using Xunit;

namespace Finaps.EventSourcing.Example.Tests;

public class BasketTests : TestsBase
{
    private const string ProductName = "TestProduct";
    private const int ProductQuantity = 10;
    private readonly HttpClient _client = Server.CreateClient();
    
    [Fact]
    public async Task Can_Create_Basket()
    {
        var response = await _client.PostAsync("baskets", null);
        var basketId = await response.ToGuid();

        Assert.NotEqual(Guid.Empty, basketId);
    }
    
    [Fact]
    public async Task Can_Get_Basket()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var response = await _client.GetAsync($"baskets/{basketId}");
        var basket = await response.AsDto<BasketDto>();
        
        Assert.NotNull(basket);
        Assert.Equal(basketId, basket!.Id);
        Assert.Empty(basket.Items);
    }
    
    [Fact]
    public async Task Can_Add_Item_To_Basket()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(productId, 1).AsHttpContent());
        var response = await _client.GetAsync($"baskets/{basketId}");
        var basket = await response.AsDto<BasketDto>();
        
        Assert.NotNull(basket);
        Assert.Single(basket!.Items);
        Assert.Equal(productId, basket.Items.First().ProductId);
        Assert.Equal(1, basket.Items.First().Quantity);
    }
    
    [Fact]
    public async Task Can_Add_Multiple_Items_To_Basket()
    {
        var product2Name = "TestProduct2";
        var product2Quantity = 5;
        
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var product1Id = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        var product2Id = await (await _client.PostAsync("products", new CreateProduct(product2Name, product2Quantity).AsHttpContent())).ToGuid();
        await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(product1Id, 1).AsHttpContent());
        await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(product2Id, 2).AsHttpContent());
        var response = await _client.GetAsync($"baskets/{basketId}");
        var basket = await response.AsDto<BasketDto>();
        
        Assert.NotNull(basket);
        Assert.Equal(2, basket!.Items.Count);
        Assert.Equal(1, basket.Items.First(x => x.ProductId == product1Id).Quantity);
        Assert.Equal(2, basket.Items.First(x => x.ProductId == product2Id).Quantity);
    }
    
    [Fact]
    public async Task Cannot_Add_Item_With_Insufficient_Stock()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        var response = await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(productId, ProductQuantity + 1).AsHttpContent());
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var response2 = await _client.GetAsync($"products/{productId}");
        var product = await response2.AsDto<ProductDto>();
        
        Assert.Empty(product!.Reservations);
    }
    
    [Fact]
    public async Task Product_Is_Reserved_After_Added_To_Basket()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        var response = await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(productId,1).AsHttpContent());
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var response2 = await _client.GetAsync($"products/{productId}");
        var product = await response2.AsDto<ProductDto>();
        
        Assert.NotEmpty(product!.Reservations);
        Assert.Equal(basketId,product.Reservations.First().BasketId);
        Assert.Equal(1,product.Reservations.First().Quantity);
    }
    
    [Fact]
    public async Task Can_Checkout_Basket()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(productId,1).AsHttpContent());
        var response = await _client.PostAsync($"baskets/{basketId}/checkout", null);
        var order = await response.AsDto<OrderDto>();
        
        Assert.NotNull(order);
        Assert.Equal(basketId, order!.BasketId);
    }
    
    [Fact]
    public async Task Product_Stock_Is_Update_After_Checkout()
    {
        var basketId = await (await _client.PostAsync("baskets", null)).ToGuid();
        var productId = await (await _client.PostAsync("products", new CreateProduct(ProductName, ProductQuantity).AsHttpContent())).ToGuid();
        await _client.PostAsync($"baskets/{basketId}/addItem", new AddProductToBasket(productId,1).AsHttpContent());
        await _client.PostAsync($"baskets/{basketId}/checkout", null);
        var response = await _client.GetAsync($"products/{productId}");
        var product = await response.AsDto<ProductDto>();
        
        Assert.NotNull(product);
        Assert.Equal(ProductQuantity - 1, product!.Quantity);
        Assert.Empty(product.Reservations);
    }
}