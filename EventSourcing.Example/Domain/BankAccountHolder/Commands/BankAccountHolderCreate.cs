using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Commands
{
    public class BankAccountHolderCreate : IBankAccountHolderCreate
    {
        public string Name { get; }
        public string EmailAddress { get; }
    }
}