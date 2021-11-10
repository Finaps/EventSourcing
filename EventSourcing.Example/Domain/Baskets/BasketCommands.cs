using System;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Baskets
{
    public record CreateBasket(Guid AggregateId) : CommandBase(AggregateId);
    public record AddProductToBasket(Guid AggregateId, Guid ProductId,int Quantity) : CommandBase(AggregateId);
    public record RemoveProductFromBasket(Guid AggregateId, Guid ProductId, int Quantity) : CommandBase(AggregateId);
    public record CheckoutBasket(Guid AggregateId) : CommandBase(AggregateId);
}