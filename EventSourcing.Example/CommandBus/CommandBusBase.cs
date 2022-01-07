using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventSourcing.Core;

namespace EventSourcing.Example.CommandBus
{
    public interface ICommandBus
    {
        Task<TAggregate> ExecuteCommand<TAggregate>(ICommand command) where TAggregate : Aggregate, new();
    }
    
    public class CommandBusBase : ICommandBus
    {
        private readonly IAggregateService _aggregateService;
        private readonly Dictionary<Type, Func<ICommand, Task<Aggregate>>> _commandHandlers = new ();

        protected CommandBusBase(IAggregateService aggregateService)
        {
            _aggregateService = aggregateService;
        }

        protected void RegisterCommandHandler<TCommand, TAggregate>(Func<TAggregate, TCommand, TAggregate> handler) where TCommand : ICommand where TAggregate : Aggregate, new()
        {
            if (_commandHandlers.ContainsKey(typeof(TCommand)))
                throw new ArgumentException($"A command handler for command: {typeof(TCommand)} is already registered");
            
            _commandHandlers.Add(typeof(TCommand), async cmd =>
            {
                var agg = await _aggregateService.RehydrateAsync<TAggregate>(cmd.AggregateId);
                handler(agg, (TCommand)cmd);
                return await _aggregateService.PersistAsync(agg);
            });
        }
        
        public async Task<TAggregate> ExecuteCommand<TAggregate>(ICommand command) where TAggregate : Aggregate, new()
        {
            if (_commandHandlers[command.GetType()] is not { } handler)
                throw new ArgumentOutOfRangeException($"No valid handler registered for command: {command.GetType()}");
            
            var aggregate = await handler(command) as TAggregate;
            return aggregate;
        }
        
        public async Task ExecuteCommand(ICommand command)
        {
            if (_commandHandlers[command.GetType()] is not { } handler)
                throw new ArgumentOutOfRangeException($"No valid handler registered for command: {command.GetType()}");
            
            await handler(command);
        }
    }
}