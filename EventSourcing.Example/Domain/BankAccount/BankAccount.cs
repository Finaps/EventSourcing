using System;
using EventSourcing.Core;
using EventSourcing.Example.Domain.BankAccount.Commands;
using EventSourcing.Example.Domain.BankAccount.Events;
using EventSourcing.Example.Domain.BankAccount.Interfaces;

namespace EventSourcing.Example.Domain.BankAccount
{
  public class BankAccount : Aggregate, IBankAccountCreate
  {
    public string Iban { get; private set;  }
    public Guid? Owner { get; private set; }
    public decimal Balance { get; private set; }
    
    public void Create(IBankAccountCreate request)
    {
      if (Version != 0) throw new InvalidOperationException("Cannot create existing bank account");

      Add(Event.Create<BankAccountCreateEvent, IBankAccountCreate>(this, request));
      
      if(request.Owner == null) return;

      LinkOwner(new BankAccountLinkOwner{Owner= request.Owner.Value});
    }
    
    public void LinkOwner(IBankAccountLinkOwner request)
    {
      if (request == null) throw new InvalidOperationException("Invalid owner id");

      Add(Event.Create<BankAccountHolderLinkedEvent, IBankAccountLinkOwner>(this, request));
    }
    public void Deposit(IBankAccountDeposit request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot deposit to a nonexistent bank account");
      
      if(Owner == null) throw new InvalidOperationException("Cannot deposit to a non owned bank account");

      Add(Event.Create<BankAccountDepositEvent, IBankAccountDeposit>(this, request));
    }

    public void Withdraw(IBankAccountWithdraw request)
    {
      if (Version == 0) throw new InvalidOperationException("Cannot withdraw from a nonexistent bank account");
      
      if(Owner == null) throw new InvalidOperationException("Cannot withdraw from a non owned bank account");
      
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