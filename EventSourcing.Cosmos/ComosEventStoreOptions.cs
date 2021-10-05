namespace EventSourcing.Cosmos
{
  public class ComosEventStoreOptions
  {
    public string ConnectionString { get; set; }
    public string Database { get; set; }
    public string Container { get; set; }
  }
}