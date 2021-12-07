using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core;

namespace EventSourcing.Example.ComandBus
{
    public interface ICommandBus
    {
        Task<TAggregate> ExecuteCommand<TAggregate>(ICommand command) where TAggregate : Aggregate, new();
    }
    
    public class CommandBusBase : ICommandBus
    {
        private readonly IAggregateService _aggregateService;
        private readonly Dictionary<Type, Func<Aggregate, ICommand, Aggregate>> _commandHandlers = new ();

        public CommandBusBase(IAggregateService aggregateService)
        {
            _aggregateService = aggregateService;
        }

        public void RegisterCommandHandler<TCommand, TAggregate>(Func<TAggregate, TCommand, TAggregate> handler) where TCommand : ICommand where TAggregate : Aggregate
        {
            if (_commandHandlers.ContainsKey(typeof(TCommand)))
                throw new ArgumentException($"A command handler for command: {typeof(ICommand)} is already registered");
            
            _commandHandlers.Add(typeof(TCommand), (agg, cmd) => handler(agg as TAggregate, (TCommand) cmd));
        }
        
        public async Task<TAggregate> ExecuteCommand<TAggregate>(ICommand command) where TAggregate : Aggregate, new()
        {
            
            if (_commandHandlers[command.GetType()] is not Func<TAggregate, ICommand, TAggregate> handler)
                throw new ArgumentOutOfRangeException($"No valid handler registered for command: {command.GetType()}");
            
            var aggregate = await _aggregateService.RehydrateAsync<TAggregate>(command.AggregateId);
            aggregate = handler(aggregate, command);
            await _aggregateService.PersistAsync(aggregate);
            return aggregate;
        }
    }
}