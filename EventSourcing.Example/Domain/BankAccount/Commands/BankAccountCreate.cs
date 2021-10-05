using System;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Commands
{
  public class BankAccountCreate : IBankAccountCreate
  {
    public Guid Owner { get; init; }
  }
}