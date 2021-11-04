using System;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Commands
{
    public class BankAccountHolderAddBankAccount : IBankAccountHolderAddBankAccount
    {
        public Guid BankAccountId { get; }
    }
}