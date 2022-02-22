namespace EventSourcing.Core.Tests.Mocks;

public record EmptyEvent : Event;

public record EmptyView : View;

public record EmptySnapshot : Snapshot;

public class EmptyAggregate : Aggregate
{
  protected override void Apply(Event e) {}
}

public class EmptyViewFactory : ViewFactory<EmptyAggregate, EmptyView>
{
  protected override EmptyView CreateView(EmptyAggregate aggregate) => new ();
}
