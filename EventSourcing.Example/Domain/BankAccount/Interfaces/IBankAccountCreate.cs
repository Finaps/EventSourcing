using System;

namespace EventSourcing.Example.Domain.BankAccount.Interfaces
{
  public interface IBankAccountCreate
  {
    Guid Owner { get; init; }
  }
}