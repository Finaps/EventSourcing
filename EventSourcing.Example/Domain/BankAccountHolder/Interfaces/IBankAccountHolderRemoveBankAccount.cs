using System;

namespace EventSourcing.Example.Domain.BankAccountHolder.Interfaces
{
    public interface IBankAccountHolderRemoveBankAccount
    {
        public Guid BankAccountId { get; }
    }
}