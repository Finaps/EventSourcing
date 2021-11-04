using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccountHolder.Interfaces;

namespace EventSourcing.Example.Domain.BankAccountHolder.Events
{
    public class BankAccountRemovedEvent : Event, IBankAccountHolderRemoveBankAccount
    {
        public Guid BankAccountId { get; }
    }
}