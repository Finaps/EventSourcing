using System;
using System.Collections.Generic;
using EventSourcing.Core;

namespace EventSourcing.Example.Commands
{
    public class CommandHandler<TAggregate> where TAggregate : Aggregate, new()
    {
        private readonly IAggregateService _aggregateService;
        private readonly Dictionary<Type, Func<TAggregate, ICommand, TAggregate>> _commandHandlers = new ();

        public CommandHandler(IAggregateService aggregateService)
        {
            _aggregateService = aggregateService;
        }

        public void RegisterCommandHandler<TCommand>(Func<TAggregate, TCommand, TAggregate> handler) where TCommand : ICommand
        {
            if (_commandHandlers.ContainsKey(typeof(TCommand)))
                throw new ArgumentException($"A command handler for command: {typeof(ICommand)} is already registered");
            
            _commandHandlers.Add(typeof(TCommand), (aggregate, command) => handler(aggregate, (TCommand) command));
        }
        
        public async void ExecuteCommand(ICommand command)
        {
            var handler = _commandHandlers[command.GetType()];
            if (handler == null)
                throw new ArgumentOutOfRangeException($"No handler registered for command: {command.GetType()}");
            
            var aggregate = await _aggregateService.RehydrateAsync<TAggregate>(command.AggregateId);
            aggregate = handler(aggregate, command);
            await _aggregateService.PersistAsync(aggregate);
        }
    }
}