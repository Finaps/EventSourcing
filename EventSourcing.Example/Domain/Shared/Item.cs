using System;

namespace EventSourcing.Example.Domain.Shared;

public record Item(Guid ProductId, int Quantity);