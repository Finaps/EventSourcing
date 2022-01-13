namespace EventSourcing.Core.Tests.Mocks;

public class MigratedAggregate: Aggregate
{
    public List<Guid> SomeIds { get; set; }= new List<Guid>();
    protected override void Apply<TEvent>(TEvent e)
    {
        switch (e)
        {
            case MigrationEventV3 v3:
                SomeIds.AddRange(v3.someIds);
                break;
        }
    }
}