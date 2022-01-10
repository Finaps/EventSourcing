using System;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Aggregates.Products;

public record ProductCreatedEvent(string Name, int Quantity) : Event();
public record ProductReservedEvent(int Quantity, Guid BasketId, TimeSpan HeldFor) : Event();
public record ReservationRemovedEvent(int Quantity, Guid BasketId) : Event();
public record ProductSoldEvent(int Quantity, Guid BasketId) : Event();
public record ProductStockAddedEvent(int Quantity) : Event();
public record InsufficientStockEvent(Guid BasketId) : Event();