using System;

namespace EventSourcing.Example.Domain.Baskets;

public record AddProductToBasket(Guid ProductId, int Quantity);
public record RemoveProductFromBasket(Guid ProductId, int Quantity);
