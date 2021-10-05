using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Commands;
using EventSourcing.Example.Domain.BankAccount.Events;

namespace EventSourcing.Example.Domain.BankAccount
{
  public class BankAccount : Aggregate
  {
    public Guid Owner { get; private set; }
    public decimal Balance { get; private set; }
    
    public void Create(BankAccountCreate request)
    {
      if (Version != 0) throw new InvalidOperationException("Cannot create existing bank account");
      
      Add<BankAccountCreateEvent>(request);
    }

    public void Deposit(BankAccountDeposit request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot deposit to a nonexistent bank account");
      
      Add<BankAccountDepositEvent>(request);
    }

    public void Withdraw(BankAccountWithdraw request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot withdraw from a nonexistent bank account");

      var e = Add<BankAccountWithdrawEvent>(request);

      if (Balance < 0) throw new InvalidOperationException(
        $"Not enough money on bank account to withdraw €{e.Amount}");
    }

    protected override void Apply(Event e)
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