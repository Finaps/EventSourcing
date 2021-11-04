using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Events
{
    public class BankAccountHolderLinkedEvent : Event, IBankAccountLinkOwner
    {
        public Guid Owner { get; }
    }
}