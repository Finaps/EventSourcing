using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount.Events
{
  public class BankAccountCreateEvent : Event, IBankAccountCreate
  {
    public string Iban { get; init; }
    public Guid Owner { get; init; }
  }
}