using System;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Commands
{
    public class BankAccountLinkOwner : IBankAccountLinkOwner
    {
        public Guid Owner { get; init; }
    }
}