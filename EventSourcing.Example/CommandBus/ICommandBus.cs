using System.Threading.Tasks;
using EventSourcing.Core;

namespace EventSourcing.Example.CommandBus;

public interface ICommandBus
{
    Task<TAggregate> ExecuteCommandAndSaveChanges<TAggregate>(ICommand command) where TAggregate : Aggregate, new();
    Task<TAggregate> ExecuteCommand<TAggregate>(ICommand command) where TAggregate : Aggregate, new();
}