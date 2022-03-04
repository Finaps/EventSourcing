# Finaps.EventSourcing

![Finaps.EventSourcing.Core](https://img.shields.io/nuget/v/Finaps.EventSourcing.Core?label=Finaps.EventSourcing.Core&style=flat-square)
![Finaps.EventSourcing.Cosmos](https://img.shields.io/nuget/v/Finaps.EventSourcing.Cosmos?label=Finaps.EventSourcing.Cosmos&style=flat-square)

Event Sourcing for .NET 6!
--------------------------

Finaps.EventSourcing is an implementation of the [Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) in .Net 6
with a focus on Validity, Clarity & Performance. The Finaps.EventSourcing package is available under the [Apache Licence 2.0](https://github.com/Finaps/EventSourcing/blob/main/LICENSE).

Currently only Azure Cosmos DB is supported. More implementations will follow in the future.

This repository is WIP, breaking API changes are likely to occur before version 1.0.0.

Table of Contents
-----------------

1. [Installation](#installation)
   1. [NuGet Package](#nuget-package)
   2. [CosmosDB Setup](#cosmos-db-setup)
   3. [Startup Configuration](#startup-configuration)
2. [Basic Usage](#basic-usage)
   1. [Define Domain Events](#1-define-domain-events)
   2. [Define Aggregate](#2-define-aggregate)
   3. [Create & Persist an Aggregate](#3-create--persist-an-aggregate)
   4. [Rehydrate & Update an Aggregate](#4-rehydrate--update-an-aggregate)
   5. [Update Aggregates in a Transaction](#5-update-aggregates-in-a-transaction)
   6. [Create & Apply Snapshots](#6-create--apply-snapshots)
   7. [Point in time Rehydration](#7-point-in-time-rehydration)
   8. [Querying Records](#8-querying-records)
   9. [Creating & Querying Projections](#9-creating--querying-projections)
3. [Example Project](#example-project)
   1. [Project](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example)
   2. [Tests](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example.Tests)

Installation
------------

### NuGet Package

[Finaps.EventSourcing.Cosmos](https://www.nuget.org/packages/Finaps.EventSourcing.Cosmos/) is available on [NuGet](https://www.nuget.org/packages/Finaps.EventSourcing.Cosmos/).

```bash
> dotnet add package Finaps.EventSourcing.Cosmos
```

### Cosmos DB Setup

Finaps.EventSourcing currently only supports [Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction).
To create a Cosmos DB Account, Database and Container, checkout the [Microsoft Documentation on Creating Cosmos DB Resources](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/create-cosmosdb-resources-portal).

For local development, once can use the Docker Cosmos Emulator for [Linux/MacOs](https://docs.microsoft.com/en-us/azure/cosmos-db/linux-emulator) or [Windows](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator-on-docker-windows).

**Important: When Creating a Cosmos DB Container to use with the Finaps.EventSourcing package, make sure to set ```PartitionKey``` equal to ```/PartitionId```.**

### Startup Configuration

In ```appsettings.json```, add the following configuration

```json
{
  "Cosmos": {
    "ConnectionString": "<Cosmos Connection String>",
    "Database": "<Cosmos Database Name>",
    "Container": "<Cosmos Container Name>"
  }
}
```

In your ```Startup.cs```, add the following configuration

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<CosmosEventStoreOptions>(Configuration.GetSection("Cosmos"));
    services.AddSingleton<IRecordStore, CosmosRecordStore>();
    services.AddSingleton<IAggregateService, AggregateService>();
}
```

Now you can use the ```IRecordStore``` and ```IAggregateService``` to power your backend!

Basic Usage
-----------

These examples show how a (very simplified) bank account could be modelled using Finaps.EventSourcing.
It show how to use three types of ```Records``` this package is concerned with: ```Events```, ```Snapshots``` and ```Projections```.

Checkout the [Example Project](#example-project) for a more thorough example on how this package can be used.

### 1. Define Domain Events

```Events``` are immutable ```Records``` that describe something that has happened to a particular ```Aggregate```.

```c#
public record BankAccountCreatedEvent : Event
{
    public string Name { get; init; }
    public string Iban { get; init; }
}

public record FundsDepositedEvent : Event
{
    public decimal Amount { get; init; }
}

public record FundsWithdrawnEvent : Event
{
    public decimal Amount { get; init; }
}

public record FundsTransferredEvent : Event
{
    public Guid DebtorAccount { get; init; }
    public Guid CreditorAccount { get; init; }
}
```

### 2. Define Aggregate

An ```Aggregate``` is an aggregation of one or more ```Events```.
The ```Aggregate.Apply(Event e)``` method contains the aggregation logic.

```c#
public class BankAccount : Aggregate
{
    // Properties are only updated by applying events
    public string Name { get; private set; }
    public string Iban { get; private set; }
    public decimal Balance { get; private set; }
    
    // This method gets called for every added Event
    protected override void Apply(Event e)
    {
        // Depending on the type of Event, we update the Aggregate
        switch (e)
        {
            case BankAccountCreatedEvent created:
                Name = created.Name;
                Iban = created.Iban;
                break;
                
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
        if (Balance < 0) throw new InvalidOperationException("Not enough funds");
    }
    
    // Convenience method for creating this account
    public void Create(string name, string iban) =>
        Add(new BankAccountCreatedEvent { Name = name, Iban = iban });
    
    // Convenience method for depositing funds to this account
    public void Deposit(decimal amount) =>
        Add(new FundsDepositedEvent { Amount = amount });
        
    // Convenience method for withdrawing events from this account
    public void Withdraw(decimal amount) =>
        Add(new FundsWithdrawnEvent { Amount = amount });
}
```

### 3. Create & Persist an Aggregate

```c#
// Create new Bank Account Aggregate
var account = new BankAccount();

// This will create a new Id
Assert.NotEqual(Guid.Empty, account.Id);

// But leave all other values default
Assert.Equal(default, account.Name);
Assert.Equal(default, account.Iban);
Assert.Equal(default, account.Balance);

// Create the Bank Account
account.Add(new BankAccountCreatedEvent { Name = "E. Vent", Iban = "SOME IBAN" });
// or alternatively, using the convenience method:
// account.Create("E. Vent", "SOME IBAN");

// Add some funds to this account
account.Deposit(100);
// which is equivalent to:
// account.Add(new FundsDepositedEvent { Amount = 100 });

// Adding Events will call the Apply method, which updates the Aggregate
Assert.Equal("E. Vent"  , account.Name);
Assert.Equal("SOME IBAN", account.Iban);
Assert.Equal(100        , account.Balance);

// Persist Aggregate, i.e. store the two newly added Events for this BankAccount
await AggregateService.PersistAsync(account);
```

### 4. Rehydrate & Update an Aggregate

When you want to update an ```Aggregate``` whose ```Events``` are already stored in the ```RecordStore```,
you'll first need to rehydrate the ```Aggregate``` from these ```Events```.

```c#
// Rehydrate existing BankAccount, i.e. reapply all stored Events to this BankAccount
var account = await AggregateService.RehydrateAsync<BankAccount>(bankAccountId);

// Then add more funds to the account
account.Deposit(50);

// Finally, Persist Aggregate. i.e. store the newly added Event(s)
await AggregateService.PersistAsync(account);
```

or alternatively, the three lines of code above can be replaced with the shorthand notation:

```c#
await AggregateService.RehydrateAndPersistAsync<BankAccount>(bankAccountId, account => account.Deposit(50));
```

### 5. Update Aggregates in a Transaction

Let's spice things up and transfer money from one bank account to another.
In such a transaction we want to ensure the transaction either entirely succeeds or entirely fails.

Here's where transactions come into play:


```c#
// Create another BankAccount
var anotherAccount = new BankAccount();
anotherAccount.Create("S. Ourcing", "ANOTHER IBAN");

// Define a transfer of funds
var transfer = new FundsTransferredEvent
{
      DebtorAccount = account.Id,
      CreditorAccount = anotherAccount.Id,
      Amount = 20
};

// Add this Event to both Aggregates
account.Add(transfer);
anotherAccount.Add(transfer);

// Persist both aggregates in a single ACID transaction.
await AggregateService.PersistAsync(new[] { account, anotherAccount });
```

### 6. Create & Apply Snapshots

When many Events are stored for a given Aggregate, rehydrating that Aggregate will get less performant.
The meaning of 'many Events' depends on backend and database hardware, but also your performance requirements.
When performance impacts are expected (or even better, measured!), ```Snapshots``` can be used to mitigate them.

To use ```Snapshots```, first define a ```Snapshot``` and a ```SnapshotFactory```.

```c#
// A Snapshot represents the full state of an Aggregate at a given point in time
public record BankAccountSnapshot : Snapshot
{
  public string Name { get; init; }
  public string Iban { get; init; }
  public decimal Balance { get; init; }
}

// The Snapshot Factory is resposible for creating a Snapshot at a given interval
public class BankAccountSnapshotFactory : SnapshotFactory<BankAccount, BankAccountSnapshot>
{
    // Create a snapshot every 100 Events
    public override long SnapshotInterval => 100;
    
    // Create a Snapshot from the Aggregate
    protected override BankAccountSnapshot CreateSnapshot(BankAccount aggregate) => new BankAccountSnapshot()
    {
        Name = aggregate.Name,
        Iban = aggregate.Iban,
        Balance = aggregate.Balance
    };
}
```

Finally, we have to apply the ````Snapshot```` in the ```Aggregate.Apply``` method:

```c#
public class BankAccount : Aggregate
{
    public string Name { get; private set; }
    public string Iban { get; private set; }
    public decimal Balance { get; private set; }
   
    protected override void Apply(Event e)
    {
        switch (e)
        {
          ...
        
          case BankAccountSnapshot snapshot:
            Name = snapshot.Name;
            Iban = snapshot.Iban;
            Balance = snapshot.Balance;
            break;
        }
        
        ...
    }
    
    ...
}
```

The ```SnapshotFactory``` will create a ```Snapshot``` every 100 ```Events```.
When rehydrating the ```Aggregate```, the latest ```Snapshot``` will be used to rehydrate the BankAccount faster.

### 7. Point in time Rehydration

Sometimes we want to get the state of a particular ```Aggregate``` at a given point in time.
This is where Event Sourcing really shines, since it is as easy as applying all events up to a certain date.
When using ```Snapshots```, the latest ```Snapshot``` before the given date 
will be used to speed up these point in time rehydrations as well.

```c#
// Query the Bank Account for the January 1th, 2022
var account = await AggregateService.RehydrateAsync<BankAccount>(bankAccountId, new DateTime(2022, 01, 01));
```

### 8. Querying Records

All previous examples with with the ```AggregateService``` class,
which provides a high level API for rehydrating and persisting ```Aggregates```.
To directly work with all ```Record``` types (```Events```, ```Snapshots``` & ```Projections```) one uses the ```RecordStore```.

Some examples of what can be done using the record store:

```c#
// Get all Events for a particular Aggregate type
var events = await RecordStore.Events                       // The RecordStore exposes Events/Snapshots/Projections Queryables
    .Where(x => x.AggregateType == nameof(BankAccount))     // Linq can be used to query all Record types
    .OrderBy(x => new { x.AggregateId, x.Index })           // Sort by Aggregate Id and Index
    .AsAsyncEnumerable()                                    // Call the AsAsyncEnumerable extension method to finalize the query
    .ToListAsync();                                         // Use any System.Linq.Async method to get the results
    
// Get latest Snapshot for a particular Aggregate
var result = await RecordStore.Snapshots
    .Where(x => x.AggregateId == myAggregateId)
    .OrderByDescending(x => x.Index)
    .AsAsyncEnumerable()
    .FirstAsync();
```

For an overview of the supported linq queries, please refer to the
[CosmosDB Linq to SQL Translation documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-query-linq-to-sql).

### 9. Creating & Querying Projections

While working with ```Aggregates```, ```Events``` and ```Snapshots``` is really powerful,
it is not well suited for querying many Aggregates at one time. This is where ```Projections``` come in.

Creating ```Projections``` works the same as creating ```Snapshots```:

```c#
// A Projection represents a 'view' for the current state of the Aggregate
public record BankAccountProjection : Projection
{
    public string Name { get; init; }
    public string Iban { get; init; }
}

// The Projection factory is responsible for creating a Projection every time the Aggregate is persisted 
public class BankAccountProjectionFactory : ProjectionFactory<BankAccount, BankAccountProjection>
{
    // This particular projection could be used for an overview page
    // We left out the balance (privacy) and made the name uppercase
    // Any transformation could be done here, e.g. to make frontend consumption easier/faster
    protected override BankAccountProjection CreateProjection(BankAccount aggregate) => new BankAccountProjection()
    {
        Name = aggregate.Name.ToUpper(),
        Iban = aggregate.Iban
    };
}
```

Projections are updated whenever the ```Aggregate``` of a particular type are persisted.
You can make as many projections for a given ```Aggregate``` type as you like.

To query ```Projections```, use the ```RecordStore``` API:

```c#
// Get first 10 BankAccount Projections, ordered by the Bank Account name
var projections = await RecordStore.GetProjections<BankAccountProjection>()
    .OrderBy(x => x.Name)
    .Skip(0)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();
```

Example Project
---------------

For a more thorough example, check out the [Example Project](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example) 
and corresponding [Example Tests](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example.Tests).