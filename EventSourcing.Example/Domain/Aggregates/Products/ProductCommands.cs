using System;
using EventSourcing.Core.Exceptions;
using EventSourcing.Example.CommandBus;

namespace EventSourcing.Example.Domain.Aggregates.Products;

public record CreateProduct(Guid AggregateId, string Name, int Quantity) : CommandBase(AggregateId);
public record Reserve(Guid AggregateId, Guid BasketId, int Quantity, TimeSpan TimeToHold) : CommandBase(AggregateId);
public record Purchase(Guid AggregateId, Guid BasketId, int Quantity) : CommandBase(AggregateId);
public record RemoveReservation(Guid AggregateId, Guid BasketId, int Quantity) : CommandBase(AggregateId);
public record AddStock(Guid AggregateId, int Quantity) : CommandBase(AggregateId);
    
    
    
    
public static class ProductCommandHandlers
{
    public static Func<Product, CreateProduct, Product> Create = (product, cmd) =>
    {
        if (product != null)
            throw new ConcurrencyException($"Order with id: {product.Id} already exists");

        product = new Product();
        product.Create(cmd.Name, cmd.Quantity);
        return product;
    };
        
    public static Func<Product, Reserve, Product> Reserve = (product, cmd) =>
    {
        if (product == null)
            throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
        product.ReserveProduct(cmd.BasketId, cmd.Quantity);
        return product;
    };
        
    public static Func<Product, Purchase, Product> Purchase = (product, cmd) =>
    {
        if (product == null)
            throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
        product.PurchaseProduct(cmd.BasketId, cmd.Quantity);
        return product;
    };
        
    public static Func<Product, RemoveReservation, Product> RemoveReservation = (product, cmd) =>
    {
        if (product == null)
            throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");
            
        product.RemoveReservation(cmd.BasketId, cmd.Quantity);
        return product;
    };
        
    public static Func<Product, AddStock, Product> AddStock = (product, cmd) =>
    {
        if (product == null)
            throw new InvalidOperationException($"Product with id: {cmd.AggregateId} does not exist");

        if (cmd.Quantity == 0)
            return product;
            
        product.AddStock(cmd.Quantity);
        return product;
    };
}