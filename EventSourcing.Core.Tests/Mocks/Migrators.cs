using EventSourcing.Core.Migrations;

namespace EventSourcing.Core.Tests.Mocks;

public class MigratorV1 : RecordMigrator<MigrationEvent, MigrationEventV2>
{
  protected override MigrationEventV2 Convert(MigrationEvent e)
  {
    return Guid.TryParse(e.someId, out var guid)
      ? new MigrationEventV2(guid)
      : new MigrationEventV2(null);
  }
}

public class MigratorV2 : RecordMigrator<MigrationEventV2, MigrationEventV3>
{
  protected override MigrationEventV3 Convert(MigrationEventV2 e)
  {
    return e.someId == null
      ? new MigrationEventV3(new List<Guid>())
      : new MigrationEventV3(new List<Guid> { e.someId.Value });
  }
}