using Finaps.EventSourcing.Example.Domain.Shared;

namespace Finaps.EventSourcing.Example.Tests.DTOs;

public record BasketDto(Guid Id, List<Item> Items, bool CheckedOut, DateTimeOffset BasketCreated, DateTimeOffset BasketExpires);