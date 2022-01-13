namespace EventSourcing.Core.Tests.Mocks;

public record MigrationEvent(string someId): Event;

public record MigrationEventV2(Guid? someId): Event;

public record MigrationEventV3(List<Guid> someIds): Event;

public class x
{
    
}