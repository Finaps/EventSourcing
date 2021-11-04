using System;

namespace EventSourcing.Example.Domain.BankAccount.Interfaces
{
    public interface IBankAccountLinkOwner
    {
        public Guid Owner { get; }
    }
}