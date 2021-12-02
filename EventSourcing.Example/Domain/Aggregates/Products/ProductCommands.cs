using System;
using EventSourcing.Example.CommandHandler;

namespace EventSourcing.Example.Domain.Aggregates.Products
{
    public record Create(Guid AggregateId, string Name, int Quantity) : CommandBase(AggregateId);
    public record Reserve(Guid AggregateId, Guid BasketId, int Quantity, TimeSpan TimeToHold) : CommandBase(AggregateId);
    public record Purchase(Guid AggregateId, Guid BasketId, int Quantity) : CommandBase(AggregateId);
    public record RemoveReservation(Guid AggregateId, Guid BasketId, int Quantity) : CommandBase(AggregateId);
    public record AddStock(Guid AggregateId, int Quantity) : CommandBase(AggregateId);
}