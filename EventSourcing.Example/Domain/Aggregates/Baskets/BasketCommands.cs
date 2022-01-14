using System;
using EventSourcing.Example.CommandBus;

namespace EventSourcing.Example.Domain.Aggregates.Baskets;

public record CreateBasket(Guid AggregateId) : CommandBase(AggregateId);
public record AddProductToBasket(Guid AggregateId, Guid ProductId, int Quantity) : CommandBase(AggregateId);
public record RemoveProductFromBasket(Guid AggregateId, Guid ProductId, int Quantity) : CommandBase(AggregateId);
public record CheckoutBasket(Guid AggregateId) : CommandBase(AggregateId);
    
    
    
public static class BasketCommandHandlers
{
    public static Func<Basket, CreateBasket, Basket> Create = (basket, _) =>
    {
        if (basket != null)
            throw new InvalidOperationException($"Basket with id: {basket.Id} already exists");

        basket = new Basket();
        basket.Create();
        return basket;
    };
        
    public static Func<Basket, AddProductToBasket, Basket> AddProductToBasket = (basket, cmd) =>
    {
        if (basket == null)
            throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
        basket.AddProduct(cmd.Quantity, cmd.ProductId);
        return basket;
    };
        
    public static Func<Basket, RemoveProductFromBasket, Basket> RemoveProductFromBasket = (basket, cmd) =>
    {
        if (basket == null)
            throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
        basket.RemoveProduct(cmd.Quantity, cmd.ProductId);
        return basket;
    };
        
    public static Func<Basket, CheckoutBasket, Basket> CheckoutBasket = (basket, cmd) =>
    {
        if (basket == null)
            throw new InvalidOperationException($"Basket with id: {cmd.AggregateId} does not exist");
            
        basket.CheckoutBasket();
        return basket;
    };
}