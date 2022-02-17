using EventSourcing.Core.Records;

namespace EventSourcing.Core.Tests.Mocks;

public record MigrationEvent(string someId): Event;

public record MigrationEventV2(Guid? someId): Event;

public record MigrationEventV3(List<Guid> someIds): Event;
public record TrivialMigrationEventOriginal(Guid someId, string someString, int someInt, decimal removedField): Event;
public record TrivialMigrationEvent(Guid someId, string someString, int? someInt, Guid? addedField): Event;