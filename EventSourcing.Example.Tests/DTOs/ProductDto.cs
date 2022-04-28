using Finaps.EventSourcing.Example.Domain.Products;

namespace Finaps.EventSourcing.Example.Tests.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int Quantity { get; set; }
    public List<Reservation> Reservations { get; set; }
}