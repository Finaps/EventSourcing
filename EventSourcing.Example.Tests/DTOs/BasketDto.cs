using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Tests.DTOs;

public class BasketDto
{
    public Guid RecordId { get; set; }
    public List<Item> Items { get; set; }
    public bool CheckedOut { get; set; }
    public DateTimeOffset BasketCreated { get; set; }
    public DateTimeOffset BasketExpires { get; set; }
}