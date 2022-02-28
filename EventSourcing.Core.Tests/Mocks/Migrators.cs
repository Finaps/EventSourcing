namespace EventSourcing.Core.Tests.Mocks;

public class MigratorV1 : EventMigrator<MigrationEvent, MigrationEventV2>
{
  protected override MigrationEventV2 Convert(MigrationEvent e)
  {
    return Guid.TryParse(e.SomeId, out var guid)
      ? new MigrationEventV2(guid)
      : new MigrationEventV2(null);
  }
}

public class MigratorV2 : EventMigrator<MigrationEventV2, MigrationEventV3>
{
  protected override MigrationEventV3 Convert(MigrationEventV2 e)
  {
    return e.SomeId == null
      ? new MigrationEventV3(new List<Guid>())
      : new MigrationEventV3(new List<Guid> { e.SomeId.Value });
  }
}