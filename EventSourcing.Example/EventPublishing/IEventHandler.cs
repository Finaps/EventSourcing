using System.Threading.Tasks;

namespace EventSourcing.Example.EventPublishing
{
    public interface IEventHandler {}
    public interface IEventHandler<in TEvent> : IEventHandler
    {
        Task Handle(TEvent @event);
    }
}