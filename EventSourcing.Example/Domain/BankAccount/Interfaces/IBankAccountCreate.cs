using System;

namespace EventSourcing.Example.Domain.BankAccount.Interfaces
{
  public interface IBankAccountCreate
  {
    string Iban { get; }
    Guid Owner { get; }
  }
}