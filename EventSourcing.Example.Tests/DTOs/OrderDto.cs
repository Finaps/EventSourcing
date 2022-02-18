namespace EventSourcing.Example.Tests.DTOs;

public class OrderDto
{
    public Guid RecordId { get; set; }
    public Guid BasketId { get; set; }
}