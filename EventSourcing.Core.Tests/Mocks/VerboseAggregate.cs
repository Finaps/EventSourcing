namespace EventSourcing.Core.Tests.Mocks
{
  public class VerboseAggregate : Aggregate
  {
    public int NumberOfAppliedEvents { get; private set; }
    public string HashDuringApply { get; private set; }
    public bool IsFinished = false;
    
    protected override void Apply<TEvent>(TEvent e)
    {
      HashDuringApply = Hash;
      NumberOfAppliedEvents++;
    }

    protected override void Finish() => IsFinished = true;
  }
}
