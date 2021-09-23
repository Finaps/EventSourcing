using System.Linq;

namespace EventSourcing.Core
{
  public interface IEventFilter<TEvent> where TEvent : Event
  {
    public IQueryable<TEvent> Filter(IQueryable<TEvent> queryable);
  }
}