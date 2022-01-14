using EventSourcing.Core.Migrations;

namespace EventSourcing.Core.Tests.Mocks;

public class MigratorV1 : EventMigratorBase<MigrationEvent, MigrationEventV2>
{
    public override MigrationEventV2 Convert(MigrationEvent e)
    {
        if (Guid.TryParse(e.someId, out var guid))
            return new MigrationEventV2(guid);

        return new MigrationEventV2(null);
    }
}

public class MigratorV2 : EventMigratorBase<MigrationEventV2, MigrationEventV3>
{
    public override MigrationEventV3 Convert(MigrationEventV2 e)
    {
        if (e.someId == null)
            return new MigrationEventV3(new List<Guid>());
        
        return new MigrationEventV3(new List<Guid> {e.someId.Value});
    }
}

// public class MigratorV3 : EventMigratorBase<MigrationEventV3, MigrationEvent>
// {
//     public override MigrationEvent Convert(MigrationEventV3 e)
//     {
//         if (e.someIds == null || e.someIds.Count == 0)
//             return new MigrationEvent("");
//         
//         return new MigrationEvent(e.someIds.First().ToString());
//     }
// }