using System;

namespace EventSourcing.Example.Domain.BankAccountHolder.Interfaces
{
    public interface IBankAccountHolderAddBankAccount
    {
        public Guid BankAccountId { get; }
    }
}