using System;

namespace EventSourcing.Example.Domain.Orders;

public record CreateOrder(Guid BasketId);