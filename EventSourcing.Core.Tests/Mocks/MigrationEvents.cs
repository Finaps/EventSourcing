namespace EventSourcing.Core.Tests.Mocks;

public record MigrationEvent(string SomeId): Event;

public record MigrationEventV2(Guid? SomeId): Event;

public record MigrationEventV3(List<Guid> SomeIds): Event;
public record TrivialMigrationEventOriginal(Guid SomeId, string SomeString, int SomeInt, decimal RemovedField): Event;
public record TrivialMigrationEvent(Guid SomeId, string SomeString, int? SomeInt, Guid? AddedField): Event;