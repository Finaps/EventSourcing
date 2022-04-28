using Finaps.EventSourcing.Example.Domain.Shared;

namespace Finaps.EventSourcing.Example.Tests.DTOs;

public class BasketDto
{
    public Guid Id { get; set; }
    public List<Item> Items { get; set; }
    public bool CheckedOut { get; set; }
    public DateTimeOffset BasketCreated { get; set; }
    public DateTimeOffset BasketExpires { get; set; }
}