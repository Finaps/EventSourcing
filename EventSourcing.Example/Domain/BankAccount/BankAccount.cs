using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Events;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount
{
  public class BankAccount : Aggregate, IBankAccountCreate
  {
    public string Iban { get; private set;  }
    public Guid Owner { get; private set; }
    public decimal Balance { get; private set; }
    
    public void Create(IBankAccountCreate request)
    {
      if (Version != 0) throw new InvalidOperationException("Cannot create existing bank account");

      Add(Event.Create<BankAccountCreateEvent, IBankAccountCreate>(this, request));
    }

    public void Deposit(IBankAccountDeposit request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot deposit to a nonexistent bank account");

      Add(Event.Create<BankAccountDepositEvent, IBankAccountDeposit>(this, request));
    }

    public void Withdraw(IBankAccountWithdraw request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot withdraw from a nonexistent bank account");

      var e = Add(Event.Create<BankAccountWithdrawEvent, IBankAccountWithdraw>(this, request));

      if (Balance < 0) throw new InvalidOperationException(
        $"Not enough money on bank account to withdraw €{e.Amount}");
    }

    protected override void Apply<TEvent>(TEvent e)
    {
      Map(e);

      switch (e)
      {
        case BankAccountWithdrawEvent withdraw:
          Balance -= withdraw.Amount;
          break;
        case BankAccountDepositEvent deposit:
          Balance += deposit.Amount;
          break;
      }
    }
  }
}