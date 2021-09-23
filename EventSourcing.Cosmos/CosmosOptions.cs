namespace EventSourcing.Cosmos
{
  public class CosmosOptions
  {
    public string ConnectionString { get; set; }
    public string Database { get; set; }
    public string Container { get; set; }
    
    public int SnapshotInterval { get; set; }
  }
}