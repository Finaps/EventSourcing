namespace EventSourcing.Cosmos
{
  public class CosmosOptions
  {
    public string ConnectionString { get; set; }
    public string Database { get; set; }
    public string EventContainer { get; set; } = "Events";
    public string AggregateContainer { get; set; } = "Aggregates";

    public int SnapshotInterval { get; set; } = 0;

  }
}