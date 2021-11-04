using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Events
{
    public class BankAccountAddedEvent : Event, IBankAccountHolderAddBankAccount
    {
        public Guid BankAccountId { get; }
    }
}