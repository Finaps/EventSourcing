using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Commands
{
    public class BankAccountHolderRemoveBankAccount : IBankAccountHolderRemoveBankAccount
    {
        public Guid BankAccountId { get; }
    }
}