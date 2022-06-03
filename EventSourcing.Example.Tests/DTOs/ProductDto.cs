using Finaps.EventSourcing.Example.Domain.Products;

namespace Finaps.EventSourcing.Example.Tests.DTOs;

public record ProductDto(Guid Id, string Name, int Quantity, List<Reservation> Reservations);