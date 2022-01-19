using System;

namespace EventSourcing.Example.Domain.Products;

public record CreateProduct(string Name, int Quantity);
public record Reserve(Guid BasketId, int Quantity, TimeSpan TimeToHold);
public record Purchase(Guid BasketId, int Quantity);
public record RemoveReservation(Guid BasketId, int Quantity);
public record AddStock(int Quantity);