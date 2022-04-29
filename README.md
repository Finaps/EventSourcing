# Finaps.EventSourcing

![Finaps.EventSourcing.EF](https://img.shields.io/nuget/v/Finaps.EventSourcing.EF?label=Finaps.EventSourcing.EF&style=flat-square)
![Finaps.EventSourcing.Cosmos](https://img.shields.io/nuget/v/Finaps.EventSourcing.Cosmos?label=Finaps.EventSourcing.Cosmos&style=flat-square)
![Finaps.EventSourcing.Core](https://img.shields.io/nuget/v/Finaps.EventSourcing.Core?label=Finaps.EventSourcing.Core&style=flat-square)

Event Sourcing for .NET 6!
--------------------------

**This repository is WIP**. Breaking API changes are likely to occur before version 1.0.0.

```Finaps.EventSourcing``` is an implementation of the [Event Sourcing Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/event-sourcing) in .NET 6
with a focus on _Validity_, _Clarity_ & _Performance_.

```Finaps.EventSourcing``` supports SQL Server, Postgres and Azure Cosmos DB databases.

All Finaps.EventSourcing packages are available under the [Apache Licence 2.0](https://github.com/Finaps/EventSourcing/blob/main/LICENSE).

Table of Contents
-----------------

1. [Installation](#installation)
   1. [Entity Framework Core](#entity-framework-core)
   2. [CosmosDB](#cosmos-db)
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
3. [Advanced Usage](#advanced-usage)
4. [Concepts](#concepts)
   1. [Records](#records)
      1. [Events](#1-events)
      2. [Snapshots](#2-snapshots)
      3. [Projections](#3-projections)
   2. [Aggregates](#aggregates)
5. [SQL vs NoSQL](#sql-vs-nosql)
6. [Example Project](#example-project)

Installation
------------

### Entity Framework Core

Alongside CosmosDB, support for relational databases is provided using [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/).

Through EF Core, ```Finaps.EventSourcing``` supports [SQL Server](https://docs.microsoft.com/en-us/sql/sql-server) & [PostgreSQL](https://www.postgresql.org/docs/current/index.html) databases.

#### NuGet Packages

[Finaps.EventSourcing.EF](https://www.nuget.org/packages/Finaps.EventSourcing.EF/) is available on [NuGet](https://www.nuget.org/packages/Finaps.EventSourcing.EF/).

```bash
> dotnet add package Finaps.EventSourcing.EF
```

And Depending on which database you are using, make sure to install the right provider

```bash
> dotnet add package Microsoft.EntityFrameworkCore.SqlServer

or

> dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

#### Database & DBContext Setup

Like most Entity Framework Core applications, the database is managed by [Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli).
The ```Finaps.EventSourcing.EF``` package adds migrations based on the Records (Events/Snapshots/Projections) you have defined and you are responsible for updating the database using them.
To access this functionality, you have to configure a [DbContext](https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/) which inherits from the ```RecordContext``` class.
You can use the ```OnModelCreating``` method to override or add new Entities to the context.

#### ASP.Net Core Configuration

Your ```DbContext``` is configured in the same way as any other, refer to the [Microsoft docs](https://docs.microsoft.com/en-us/ef/core/dbcontext-configuration/) on how to do this,
but your configuration could look something like this:

```json5
// appsettings.json
{
   "ConnectionStrings": {
      "RecordStore": "<SQL Server/Postgres Connection String>"
   }
}
```

```c#
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{    
    services.AddDbContext<ViewContext>(options =>
    {
      options.UseSqlServer(configuration.GetConnectionString("RecordStore"));
      // or
      options.UseNpgsql(configuration.GetConnectionString("RecordStore"));
    });
      
    services.AddScoped<IRecordStore, YourEntityFrameworkRecordStore>();
    services.AddScoped<IAggregateService, AggregateService>();
}
```

Now you can use the ```EntityFrameworkRecordStore``` and ```AggregateService``` to power your backend!

#### DateTimeOffset in Postgres 

Because all records use the ```DateTimeOffset``` data type for ```Record.Timestamp``` and 
[Postgres does not support storing offsets natively](https://www.npgsql.org/doc/types/datetime.html),
we have to explicitly tell Postgres it should store ```DateTimeOffset``` as UTC without offset information.

This can be done by creating a [Custom Value Conversion in EF Core](https://docs.microsoft.com/en-us/ef/core/modeling/value-conversions?tabs=data-annotations) or by calling:

```c#
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
```

### Cosmos DB

#### Nuget Package

[Finaps.EventSourcing.Cosmos](https://www.nuget.org/packages/Finaps.EventSourcing.Cosmos/) is available on [NuGet](https://www.nuget.org/packages/Finaps.EventSourcing.Cosmos/).

```bash
> dotnet add package Finaps.EventSourcing.Cosmos
```

#### Database Setup

Finaps.EventSourcing supports [Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction).
To create a Cosmos DB Account, Database and Container, checkout the [Microsoft Documentation on Creating Cosmos DB Resources](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/create-cosmosdb-resources-portal).

For local development, one can use the Docker Cosmos Emulator for [Linux/macOS](https://docs.microsoft.com/en-us/azure/cosmos-db/linux-emulator) or [Windows](https://docs.microsoft.com/en-us/azure/cosmos-db/local-emulator-on-docker-windows).

When Creating a Cosmos DB Container to use with ```Finaps.EventSourcing.Cosmos```, make sure to set ```PartitionKey``` equal to ```/PartitionId```.

#### ASP.Net Core Configuration

```json5
// appsettings.json

{
  "Cosmos": {
    "ConnectionString": "<Cosmos Connection String>",
    "Database": "<Cosmos Database Name>",
    "Container": "<Cosmos Container Name>"
  }
}
```

```c#
// Startup.cs

public void ConfigureServices(IServiceCollection services)
{
    services.Configure<CosmosEventStoreOptions>(Configuration.GetSection("Cosmos"));
    services.AddSingleton<IRecordStore, CosmosRecordStore>();
    services.AddSingleton<IAggregateService, AggregateService>();
}
```

Now you can use the ```CosmosRecordStore``` and ```AggregateService``` to power your backend!

Basic Usage
-----------

These examples show how a (very simplified) bank account could be modelled using ```Finaps.EventSourcing```.
It shows how to use the three types of Records this package is concerned with: Events, Snapshots and Projections.
These examples work with both ```Finaps.EventSourcing.Cosmos``` and ```Finaps.EventSourcing.EF```

Checkout the [Example Project](#example-project) for a more thorough example on how this package can be used.

### 1. Define Domain Events

Events are immutable Records that describe something that has happened to a particular Aggregate.

```c#
public record BankAccountCreatedEvent(string Name, string Iban) : Event<BankAccount>;

public record FundsDepositedEvent(decimal Amount) : Event<BankAccount>;

public record FundsWithdrawnEvent(decimal Amount) : Event<BankAccount>;

public record FundsTransferredEvent(decimal Amount, Guid DebtorAccount, Guid CreditorAccon : Event<BankAccount>;
```

Note:
- Events are scoped to a particular Aggregate class, specified by ```Event<TAggregate>```
- Events should be immutable, so either:
  - use [positional records](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/records)**
  - use ```{ get; init; }``` accessors

### 2. Define Aggregate

An Aggregate is an aggregation of one or more Events.

```c#
public class BankAccount : Aggregate<BankAccount>
{
    public string Name { get; private set; }
    public string Iban { get; private set; }
    public decimal Balance { get; private set; }
    
    protected override void Apply(Event<BankAccount> e)
    {
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
                
            case BankAccountFundsTransferredEvent transfer when transfer.DebtorAccount == Id:
                 Balance -= transfer.Amount;
                 break;
            case BankAccountFundsTransferredEvent transfer when transfer.CreditorAccount == Id:
                 Balance += transfer.Amount;
                 break;
            case BankAccountFundsTransferredEvent:
                throw new InvalidOperationException("Not debtor nor creditor of this transaction");
            
            default:
              throw new ArgumentOutOfRangeException(nameof(e));
        }
        
        // An error is thrown if any event would cause the bank account balance to drop below 0
        if (Balance < 0) throw new InvalidOperationException("Not enough funds");
    }
}
```

Note:
- The ```Aggregate.Apply``` method contains the logic for aggregating Events.
  - Using C# 7+ pattern matching, logic can be added based on Event type and Aggregate State
- Aggregate properties should only be updated by applying Events, hence the ```{ get; private set; }``` accessors.
- Aggregates reference themselves, specified by ```Aggregate<TAggregate>```, 
  which enables static type checking for the ```Aggregate.Apply(Event<TAggregate>)``` method.

### 3. Create & Persist an Aggregate

```c#
// Create new Bank Account Aggregate
var account = new BankAccount();

// This created a new Aggregate.Id
Assert.NotEqual(Guid.Empty, account.Id);

// But left all other values default
Assert.Equal(default, account.Name);
Assert.Equal(default, account.Iban);
Assert.Equal(default, account.Balance);

// Create the Bank Account by applying an Event
account.Apply(new BankAccountCreatedEvent("E. Vent", "SOME IBAN");

// Add some funds to this account using a convenience method
account.Apply(new FundsDepositedEvent(100));

// By calling the Apply method, the Aggregat is now updated
Assert.Equal("E. Vent"  , account.Name);
Assert.Equal("SOME IBAN", account.Iban);
Assert.Equal(100        , account.Balance);

// Finally: Persist the Aggregate
// This will store the newly added Events for this BankAccount in the ```IRecordStore```
await AggregateService.PersistAsync(account);
```

### 4. Rehydrate & Update an Aggregate

When you want to update an existing Aggregate, you'll first need to rehydrate the Aggregate:

```c#
// Rehydrate existing BankAccount, i.e. apply all stored Events to this BankAccount
var account = await AggregateService.RehydrateAsync<BankAccount>(bankAccountId);

// Then add more funds to the account
account.Apply(new FundsDepositedEvent(50));

// Finally, Persist Aggregate. i.e. store the newly added Event(s)
await AggregateService.PersistAsync(account);
```

or alternatively, the three lines of code above can be replaced with the shorthand notation:

```c#
await AggregateService.RehydrateAndPersistAsync<BankAccount>(bankAccountId,
    account => account.Apply(new FundsDepositedEvent(50));
```

### 5. Update Aggregates in a Transaction

Let's spice things up and transfer money from one bank account to another.
In such a transaction we want to ensure the transaction either entirely succeeds or entirely fails.

Here's where transactions come into play:

```c#
// Create another BankAccount
var anotherAccount = new BankAccount();

anotherAccount.Apply(new BankAccountCreatedEvent("S. Ourcing", "ANOTHER IBAN");

// Define a transfer of funds
var transfer = new FundsTransferredEvent(20, account.Id, anotherAccount.Id);

// Add this Event to both Aggregates
account.Apply(transfer);
anotherAccount.Apply(transfer);

// Persist both aggregates in a single ACID transaction.
await AggregateService.PersistAsync(new[] { account, anotherAccount });
```

### 6. Create & Apply Snapshots

When many Events are stored for a given Aggregate, rehydrating that Aggregate will get more expensive.
The meaning of 'many Events' will depend on backend and database hardware, but also your performance requirements.
When performance impacts are expected (or even better, measured!), Snapshots can be used to mitigate them.

Snapshots work by storing a copy of the Aggregate state every n Events. 
When rehydrating, the latest Snapshot and all Events after that will be used, instead of applying all Events from scratch.

To use Snapshots, first define a ```Snapshot```:

```c#
// A Snapshot represents the full state of an Aggregate at a given point in time
public record BankAccountSnapshot(string Name, string Iban, decimal Balance) : Snapshot<BankAccount>;
```

Note:
- Like Events, Snapshots are scoped to an Aggregate, specified using ```Snapshot<TAggregate>```
- Like Events, Snapshots should be immutable: use _positional records_ or ```{ get; init; }``` accessors.

Next, define a ```SnapshotFactory```:

```c#
// The Snapshot Factory is resposible for creating a Snapshot at a given Event interval
public class BankAccountSnapshotFactory : SnapshotFactory<BankAccount, BankAccountSnapshot>
{
    // Create a snapshot every 100 Events
    public override long SnapshotInterval => 100;
    
    // Create a Snapshot from the Aggregate
    protected override BankAccountSnapshot CreateSnapshot(BankAccount aggregate) => 
        new BankAccountSnapshot(aggregate.Name, aggregate.Iban, aggregate.Balance);
}
```

When Persisting an Aggregate using the ```AggregateService``` a ```Snapshot``` will be created when the ```SnapshotInterval``` is exceeded.
This means that Snapshots will not necessarily be created at exactly ```SnapshotInterval``` increments when applying more Events than one at a time.

After creating the ```Snapshot``` and ```SnapshotFactory```, we have to apply the ````Snapshot```` in the ```Aggregate.Apply``` method:

```c#
public class BankAccount : Aggregate<BankAccount>
{
    public string Name { get; private set; }
    public string Iban { get; private set; }
    public decimal Balance { get; private set; }
   
    protected override void Apply(Snapshot<BankAccount> e)
    {
        switch (e)
        {
          case BankAccountSnapshot snapshot:
            Name = snapshot.Name;
            Iban = snapshot.Iban;
            Balance = snapshot.Balance;
            break;
        }
    }
}
```

### 7. Point in time Rehydration

Sometimes we want to get the historical state of a particular Aggregate at a given point in time.
This is where Event Sourcing really shines, since it is as easy as applying all Events up to a certain date.
When using Snapshots, the latest ```Snapshot``` before the given date 
will be used to speed up these point in time rehydrations as well.

```c#
// Query the Bank Account for the January 1th, 2022
var account = await AggregateService.RehydrateAsync<BankAccount>(bankAccountId, new DateTime(2022, 01, 01));
```

### 8. Querying Records

All previous examples use the ```AggregateService``` class,
which provides a high level API for rehydrating and persisting Aggregates.
To directly interact with ```Record``` types (Events, Snapshots & Projections), one can use the ```IRecordStore```.

Some examples of what can be done using the record store:

```c#
// Get all Events for a particular Aggregate type
var events = await RecordStore.GetEvents<BankAccount>()     // The RecordStore exposes Events/Snapshots/Projections Queryables
    .Where(x => x.AggregateId == myAggregateId)             // Linq can be used to query all Record types
    .OrderBy(x => x.Index)                                  // Order by Aggregate Index
    .AsAsyncEnumerable()                                    // Call the AsAsyncEnumerable extension method to finalize the query
    .ToListAsync();                                         // Use any System.Linq.Async method to get the result you want
    
// Get latest Snapshot for a particular Aggregate
var result = await RecordStore.GetSnapshots<BankAccount>()
    .Where(x => x.AggregateId == myAggregateId)
    .OrderByDescending(x => x.Index)
    .AsAsyncEnumerable()
    .FirstAsync();
```

Not All Linq operations are supported by CosmosDB. For an overview of the supported linq queries in CosmosDB, please refer to the
[CosmosDB Linq to SQL Translation documentation](https://docs.microsoft.com/en-us/azure/cosmos-db/sql/sql-query-linq-to-sql).

### 9. Creating & Querying Projections

While Event Sourcing is really powerful, it is not well suited for querying many Aggregates at one time.
By creating an easily queryable read-only view of an Aggregate, Projections try to tackle this problem.

Creating Projections works the same as creating Snapshots: just define a ```Projection``` and a ```ProjectionFactory```:

```c#
// A Projection represents the current state of an Aggregate
public record BankAccountProjection(string Name, string Iban) : Projection;

// The Projection factory is responsible for creating a Projection every time the Aggregate is persisted 
public class BankAccountProjectionFactory : ProjectionFactory<BankAccount, BankAccountProjection>
{
    // This particular projection could be used for e.g. an overview page
    // We left out 'Balance' (privacy reasons) and made 'Name' uppercase
    // Any transformation could be done here, e.g. to make frontend consumption easier/faster
    protected override BankAccountProjection CreateProjection(BankAccount aggregate) => 
        new BankAccountProjection(aggregate.Name.ToUpper(), aggregate.Iban);
}
```

Projections are updated whenever the Aggregate of a particular type are persisted.
You can make as many Projections for a given Aggregate type as you like.

To query Projections, use the ```RecordStore``` API:

```c#
// Get first 10 BankAccount Projections, ordered by the Bank Account name
var projections = await RecordStore.GetProjections<BankAccountProjection>()
    .OrderBy(x => x.Name)
    .Skip(0)
    .Take(10)
    .AsAsyncEnumerable()
    .ToListAsync();
```

Advanced Usage
--------------

### 1. Aggregate References

**Note: currently this feature is only available in ```Finaps.EventSourcing.EF```**

Aggregates don't usually live in a vacuum, but are related to other Aggregates.
However, because Events are the source of truth and Aggregates are never directly persisted,
defining foreign keys to ensure data integrity is less trivial than in non-EventSourced systems.

How do we, in the example below, ensure that ```PostAggregate.BlogId``` is actually valid?

```c#
public class Blog : Aggregate<Blog>
{
    public string Name { get; private set; }
}

public class Post : Aggregate<Post>
{ 
    public Guid BlogId { get; private set; }
    public string Content { get; private set; }
}
```

We can solve this by validating all Events that contain ```BlogId```

```c#
public record PostCreated(Guid BlogId, string Content) : Event<Post>;
```

To do this, add the following line of code for every Aggregate reference to the ```DbContext.OnModelCreating```:

```c#
builder.AggregateReference<PostCreated, Blog>(x => x.BlogId);
```

This creates a relation between the ```PostCreated``` Event and the first Event of the referenced ```Blog```.

This technique can be used, alongside other techniques, to increase the data integrity of your application.

Concepts
--------

### Records

This package stores three types of [Records]("https://github.com/Finaps/EventSourcing/blob/main/EventSourcing.Core/Records/Record.cs") using the
[```IRecordStore```]("https://github.com/Finaps/EventSourcing/blob/main/EventSourcing.Core/Services/RecordStore/IRecordStore.cs"):
[Events]("https://github.com/Finaps/EventSourcing/blob/main/EventSourcing.Core/Records/Event.cs"),
[Snapshots]("https://github.com/Finaps/EventSourcing/blob/main/EventSourcing.Core/Records/Snapshot.cs") and
[Projections]("https://github.com/Finaps/EventSourcing/blob/main/EventSourcing.Core/Records/Projection.cs").

Records are always defined with respect to an Aggregate.

The abstract base ```Record``` is defined below:

```c#
public abstract record Record
{
  public RecordKind Kind { get; }                   // = Event | Snapshot | Projection
  public string Type { get; init; }                 // = nameof(<MyRecordType>)
  public string? AggregateType { get; init; }       // = nameof(<MyAggregateType>)
    
  public Guid PartitionId { get; init; }            // = Aggregate.PartitionId
  public Guid AggregateId { get; init; }            // = Aggregate.Id
  public Guid RecordId { get; init; }               // = Guid.NewGuid()
  
  public DateTimeOffset Timestamp { get; init; }    // Event/Snapshot/Projection creation time
}
```

#### 1. Events

Events are Records that describe what happened to an Aggregate.
They are added to an append only store and form the source of truth for an Aggregate.

The base ```Event``` is defined below:

```c#
public record Event : Record
{
  public long Index { get; init; }  // The index of this Event in the Event Stream
}
```

#### 2. Snapshots

Snapshots are Events that describe the complete state of an Aggregate at a particular ```Event``` index.
Snapshots can be used to speed up the rehydration of Aggregates.

The base ```Snapshot``` is defined below:

```c#
public record Snapshot : Event;
```

#### 3. Projections

Projections are Records that describe the current state of an Aggregate (and hence the ```Event``` stream).
Projections can be used to speed up queries, especially those involving many Aggregates at the same time.

The base ```Projection``` is defined below:

```c#
public record Projection : Record
{
  public string? FactoryType { get; init; }     // = nameof(<MyProjectionFactory>)
  public long Version { get; init; }            // = Aggregate.Version
  
  public string Hash { get; init; }             // Projection Hash Code, see "Updating Projections"
  public bool IsUpToDate { get; }               // True if Projection is up to date
}
```

##### Updating Projections

Unlike Events, Projections are not a source of truth, but depend on the following data:
1. The ```Event``` stream
2. The ```Aggregate.Apply``` logic
3. The ```ProjectionFactory.CreateProjection``` logic

In order to accurately reflect the current state, ```Projection```s have to be updated whenever any of these data changes.

The first point, the ```Event``` stream, is trivial to solve: The ```AggregateService``` will simply update the ```Projection``` whenever Events are persisted.

The last two points are less trivial, since they rely on user code.
To provide a solution, the ```Projection.Hash``` stores a hash representation of the [IL Bytecode]("https://en.wikipedia.org/wiki/Common_Intermediate_Language") of the methods that define Projections.
When querying projections, we can compare the stored hash to the current hash to see whether the projection was created using up to date code.
```Projection.IsUpToDate``` reflects this comparison.

Now we know whether a ```Projection``` is out of date, we can actually update it using the following methods:
1. Simply ```RehydrateAndPersist``` the aggregate with the corresponding ```AgregateType```, ```PartitionId``` and ```AggregateId```.
2. Use the ```ProjectionUpdateService``` to bulk update may Projections at once.

### Aggregates

Aggregates are the result of applying one or more Events.

The base Aggregate is defined below:

```c#
public abstract class Aggregate
{
  public string Type { get; init; }             // = nameof(<MyAggregateType>)
  public Guid PartitionId { get; init; }        // = Guid.Empty (Can be used to partition data)
  public Guid Id { get; init; }                 // Unique Aggregate Identifier
  public long Version { get; private set; }     // The number of Events applied to this Aggregate
  
  protected abstract void Apply(Event e);       // Logic to apply Events
}
```

SQL vs NoSQL
------------

```Finaps.EventSourcing.Core``` supports both SQL (SQL Server, Postgres) and NoSQL (CosmosDB) databases.
While the same ```IRecordStore``` API is exposed for all databases, there are differences.
Through the topics _Storage_, _Integrity_, _Migrations_ and _Performance_ their respective features are covered.

### Storage

This package stores Events, Snapshots and Projections,
but the way they are stored differs between NoSQL and SQL databases.

Consider the following Events:

```c#
public record BankAccountCreatedEvent(string Name, string Iban) : Event<BankAccount>;

public record BankAccountFundsDepositedEvent(decimal Amount) : Event<BankAccount>;
```

#### NoSQL Record Representation

For NoSQL, Events, Snapshots and Projections are stored as JSON in the same collection,
which allows for great flexibility when it comes to creating, updating and querying them:
- No database migrations have to be done
- Arbitrary or changing data can be stored and queried

The NoSQL JSON representation of the Bank Account Events mentioned above will look like this:

```json5
[{
   "AggregateType": "BankAccount",
   "Type": "BankAccountCreatedEvent",
   "Kind": 1, // RecordKind.Event

   // Unique Id, encoding <Kind>|<AggregateId>[<Index>]
   "id": "Event|f543d76a-3895-48e2-a836-f09d4a00cd7f[0]",
   
   "PartitionId": "00000000-0000-0000-0000-000000000000",
   "AggregateId": "f543d76a-3895-48e2-a836-f09d4a00cd7f",
   "Index": 0,
   
   "Timestamp": "2022-03-07T15:29:19.941474+01:00",
   
   "Name": "E. Sourcing",
   "Iban": "SOME IBAN"
}, {
   "AggregateType": "BankAccount",
   "Type": "BankAccountFundsDepositedEvent",
   "Kind": 1, // RecordKind.Event

   // Unique Id, encoding <Kind>|<AggregateId>[<Index>]
   "id": "Event|f543d76a-3895-48e2-a836-f09d4a00cd7f[1]",
   
   "PartitionId": "00000000-0000-0000-0000-000000000000",
   "AggregateId": "f543d76a-3895-48e2-a836-f09d4a00cd7f",
   "Index": 1,

   "Timestamp": "2022-03-07T15:29:19.942085+01:00",
   
   "Amount": 100,
}]
```

Snapshots and Projections are stored similarly.

#### SQL Record Representation

SQL is a bit less flexible when storing Events, Snapshots:

- [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/) have to be created and applied every time you create/update ```Event```, ```Snapshot``` and ```Projection``` models.
- Events and Snapshots are stored in a table per Aggregate Type using [Table per Hierarchy](https://docs.microsoft.com/en-us/ef/core/modeling/inheritance#table-per-hierarchy-and-discriminator-configuration).
  - **pro**: querying is efficient, since all Events for a given Aggregate are in one table and no joins are needed to rehydrate an Aggregate.
  - **con**: there will be redundant ```NULL``` columns when they are not applicable for a given Event Type.

The SQL Database representation of the Bank Account Events mentioned above will be:

| PartitionId                          | AggregateId                          | Index | AggregateType | Type                            | Timestamp                          | Name    | IBAN      | Amount |
|--------------------------------------|--------------------------------------|-------|---------------|---------------------------------|------------------------------------|---------|-----------|--------|
| 00000000-0000-0000-0000-000000000000 | d85e6b59-add6-46bd-bae9-f7aa0f3140e5 | 0     | BankAccount   | BankAccountCreatedEvent         | 2022-04-19 12:16:41.213708 +00:00  | E. Vent | SOME IBAN | NULL   |
| 00000000-0000-0000-0000-000000000000 | d85e6b59-add6-46bd-bae9-f7aa0f3140e5 | 1     | BankAccount   | BankAccountFundsDepositedEvent  | 2022-04-19 12:16:41.215338 +00:00  | NULL    | NULL      | 100    |

Projections are stored in a unique table per ```Projection``` type.

### Integrity

To ensure data integrity in the context of Event Sourcing one has to:

1. validate Events
2. validate Events w.r.t. Aggregate State

While both validations can be done using C# code in e.g. the ```Aggregate.Apply``` method,
EF Core adds the option to validate Events at database level using 
[Check Constraints](https://github.com/efcore/EFCore.CheckConstraints),
[Unique Constraints](https://docs.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations#index-uniqueness)
[Foreign Key Constraints](https://docs.microsoft.com/en-us/ef/core/modeling/relationships)
and [AggregateReferences](#1-aggregate-references).

### Migrations

When developing applications, updates to Event models are bound to happen.
Depending on which database powers your EventSourcing (NoSQL ```Finaps.EventSourcing.Cosmos``` or SQL ```Finaps.EventSourcing.EF```),
special care needs to be taken in order ensure backwards compatibility.

#### NoSQL

When updating Event models using the ```Finaps.EventSourcing.Cosmos``` package, 
all existing Events will remain the way they were written to the database initially.
Your code has to handle both the original as well as the updated Event models.
The following strategies can be used:

1. When **adding properties** to an Event record, consider making these properties [nullable](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nullable-value-types):
   this will ensure existing events without these properties are handled correctly in your application logic.
   You can also specify a default value for the property right on the Event record.

2. When **removing properties** from an Event model, no special care has to be taken, they will simply be ignored by the JSON conversion.

3. When **drastically changing** your Event model, consider making an entirely new Event model instead and handle both the old and the new in the ```Aggregate.Apply``` method.

4. When **changing data types**, ensure that they map to the same json representation. Be very careful when doing this.

#### SQL

When updating Event models using the ```Finaps.EventSourcing.EF``` package,
all existing Events will be updated with when applying [Entity Framework Core Migrations](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/).
Special care has to be taken to not change existing Event data in the database when changing Event models.

For SQL, most NoSQL strategies mentioned above are also applicable, however:

5When **adding constraints**, you can choose to validate them against all existing Events in the database, allowing you to reason over the validity of all Events as a whole.

### Performance

TODO: Performance Testing & Metrics


Example Project
---------------

For a more thorough example using the CosmosDB database, check out the [Example Project](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example)
and corresponding [Example Tests](https://github.com/Finaps/EventSourcing/tree/main/EventSourcing.Example.Tests).
