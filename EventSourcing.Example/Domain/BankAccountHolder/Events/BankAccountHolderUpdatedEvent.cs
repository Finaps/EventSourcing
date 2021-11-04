using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Events
{
    public class BankAccountHolderUpdatedEvent : Event, IBankAccountHolderUpdate
    {
        public string Name { get; }
        public string EmailAddress { get; }
    }
}