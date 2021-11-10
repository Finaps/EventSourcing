using System;
using EventSourcing.Example.Commands;

namespace EventSourcing.Example.Domain.Products
{
    public record Create(Guid AggregateId, string Name, int Quantity) : CommandBase(AggregateId);
    public record Reserve(Guid AggregateId, Guid BasketId, int Quantity, TimeSpan TimeToHold) : CommandBase(AggregateId);
    public record Purchase(Guid AggregateId, Guid BasketId, int Quantity) : CommandBase(AggregateId);
    public record AddStock(Guid AggregateId, int Quantity) : CommandBase(AggregateId);
}