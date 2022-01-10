# Finaps.EventSourcing

![Finaps.EventSourcing.Core](https://img.shields.io/nuget/v/Finaps.EventSourcing.Core?label=Finaps.EventSourcing.Core&style=flat-square)
![Finaps.EventSourcing.Cosmos](https://img.shields.io/nuget/v/Finaps.EventSourcing.Cosmos?label=Finaps.EventSourcing.Cosmos&style=flat-square)

Event Sourcing for .NET 6!
--------------------------

This repository contains an implementation of the Event Sourcing pattern in .NET 6.

Currently an Azure Cosmos DB backend has been implemented, but more implementations will follow in the future.

This repository is WIP, breaking API changes are likely to occur before version 1.0.0.

Example
-------

This example shows how a (very simplified) bank account could be modelled using Finaps.EventSourcing

### 1. Define Some Domain Events

Events are immutable data structures that describe something that has happened to an Aggregate.

```c#
public record FundsEvent : Event
{
    public decimal Amount { get; init; }
}

public record FundsDepositedEvent : FundsEvent;
public record FundsWithdrawnEvent : FundsEvent;

public record FundsTransferredEvent : FundsEvent
{
    public Guid DebtorAccount { get; init; }
    public Guid CreditorAccount { get; init; }
}
```

### 2. Define an Aggregate for these Events

An Aggregate is an aggregation of an Event stream

```c#
public class BankAccount : Aggregate
{
    // These fields are are 'aggregations' of events applied to this bank account
    public List<FundsEvent> History { get; } = new();
    public decimal Balance { get; private set; }
    
    public void Deposit(decimal amount) =>
        Add(new FundsDepositedEvent { Amount = amount });
        
    public void Withdraw(decimal amount) =>
        Add(new FundsWithdrawnEvent { Amount = amount });
    
    // This method gets called for every event applied to this backaccount
    protected override void Apply<TEvent>(TEvent e)
    {
        // The Bank Account Aggregate is updated according to the Applied Event
        switch (e)
        {
            case FundsDepositedEvent deposit:
                Balance += deposit.Amount;
                break;
            case FundsWithdrawnEvent withdraw:
                Balance -= withdraw.Amount;
                break;
            case FundsTransferredEvent transfer:
                if (Id == transfer.DebtorAccount)
                    Balance -= transfer.Amount;
                else if (Id == transfer.CreditorAccount)
                    Balance += transfer.Amount;
                else
                    throw new InvalidOperationException("Not debtor nor creditor of this transaction");
                break;
        }
        
        // An error is thrown if any event would cause the bank account balance to drop below 0
        if (Balance < 0)
            throw new InvalidOperationException("Not enough funds");
        
        // Update the transaction history
        if (e is FundsEvent transaction)
            History.Add(transaction);
    }
}
```

### 3. Create and Persist an Aggregate

```c#
// Create new Bank Account
var account = new BankAccount();

// Add some funds to this account
account.Deposit(100);

// Persist Aggregate by storing all Events added to this Aggregate
await AggregateService.PersistAsync(account);
```

### 4. Update an Aggregate
```c#
// Rehydrate Existing Bank Account
var account = await AggregateService.RehydrateAsync<BankAccount>(bankAccountId);

// Add funds to the account
account.Deposit(50);

// Persist Aggregate
await AggregateService.PersistAsync(account);
```

or alternatively:

```c#
await AggregateService.RehydrateAndPersistAsync<BankAccount>(bankAccountId, account => account.Deposit(50));
```

### 5. Update Multiple Aggregates in a single Transaction

```c#
var anotherAccount = new BankAccount();

var transfer = new FundsTransferredEvent
{
      DebtorAccount = account.Id,
      CreditorAccount = anotherAccount.Id,
      Amount = 20
};

account.Add(transfer);
anotherAccount.Add(transfer);

// This attempts to save both aggregates in a single transaction
// If something fails (e.g. concurrency) nothing will be persisted
await AggregateService.PersistAsync(new[] { account, anotherAccount });
```
