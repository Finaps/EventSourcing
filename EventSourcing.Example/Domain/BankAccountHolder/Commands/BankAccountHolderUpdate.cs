using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Commands
{
    public class BankAccountHolderUpdate : IBankAccountHolderUpdate
    {
        public string Name { get; }
        public string EmailAddress { get; }
    }
}