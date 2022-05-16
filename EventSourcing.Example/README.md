Example of  a webshop backend using Finaps.EventSourcing
--------------------------

This project is an example of a webshop backend service using ```Finaps.EventSourcing``` to eventsource it's aggregates using Cosmos DB or Postgres.

Keep in mind that this service is not meant to be used as a webshop backend but rather to illustrate the strengths of EventSourcing and in particular ```Finaps.EventSourcing``` for this solution.

The project is a simple AspNetCore service containing code that mostly describes behavior and uses ```Finaps.EventSourcing``` for most of it's infrastructure.

Infrastructure
-------
To set up the infrastructure ``` EventSourcingStartup.cs ``` is being called from ``` Startup.cs ```.

The following line is added in ``` EventSourcingStartup.cs ``` to set up the infrastructure to store and query events from the Cosmos DB:
```c#
services.AddEventSourcing(configuration.GetSection("Cosmos"));
```
And the following lines are added when using with a Postgres DB:
```c#
services.AddDbContext<RecordContext, ExampleContext>(options =>
{
    options.UseNpgsql(configuration.GetConnectionString("RecordStore"));
});
services.AddEventSourcing<ExampleContext>();
```
We can now request the ```AggregateService``` whenever we need to recalculate or persist the state of an aggregate.

Aggregates
-------

### 1. Product

The product aggregate represents a product which has a name, a stock quantity and possible reservations.

The following changes, which are described by events, can occur for a product
- ```ProductCreatedEvent```
- ```ProductReservedEvent```
- ```ReservationRemovedEvent```
- ```ProductSoldEvent```
- ```ProductStockAddedEvent```

Since a lot of old ```ProductReservedEvent```'s or ```ReservationRemovedEvent```'s won't be relevant for the current state of the aggregate, a snapshot will be created after a certain amount of events.
Creating a checkpoint so that not all events need to be used to rehydrate the product state.

### 2. Basket

The basket aggregate represents a shopping basket containing products

The following events can occur for a basket
- ```BasketCreatedEvent```
- ```ProductAddedToBasketEvent```
- ```ProductRemovedFromBasketEvent```
- ```BasketCheckedOutEvent```

If a basket is checked out (i.e. ```BasketCheckedOutEvent``` has occured) or expired, no new events can happen for that basket

### 3. Order
The order aggregate is created whenever a basket has been checked out. For now the only event that occurs on this aggregate is
- ```OrderCreatedEvent```

But one can expand this with events that describe changes that typically occur on an order (e.g. ```OrderShippedEvent```, ```OrderCompletedEvent```, ..)

Interactions
-------
Currently the only way to interact with these aggregates is through controller methods. For each aggregate there is a controller.

### 1. OrderController
For now only contains a ```GET``` to get an existing order.
### 2. ProductController
Should only be available to webshop employees since this controller can be used to ```POST``` a new product, ```GET``` current state of a product (e.g. to view current stock) and ```POST``` to update a product's stock.
### 3. BasketController

Contains methods to ```POST``` a new basket and ```GET``` a basket but also more interesting methods, namely:
#### ```AddItemToBasket```
This action determines if a product has enough stock and then Reserves the product and adds the item to the basket. This means that we are trying to save changes on two aggregates.

We cannot have that an item is being added to the basket while the production reservation failed (in case some other basket reserved the product and was slightly faster).
Therefore the persistence must succeed either for both aggregates or none of them which is exactly what happens when passing them in:
```c#
await _aggregateService.PersistAsync(new List<Aggregate> { product, basket });
```

#### ```CheckoutBasket```
We need to interact with all products in the basket, the basket itself and the new created order and therefore we will create an aggregate transaction:
```c#
var transaction = _aggregateService.CreateTransaction();
```
First we try to purchase each of the product in the basket (i.e. add ```ProductSoldEvent```) and add the update product to the transaction
```c#
transaction.Add(product);
```
Then we checkout the basket (```BasketCheckedOutEvent```) and create a new order (```OrderCreatedEvent```) and we add both aggregates to the transaction.
Again, we need the persistence of all changes to succeed or persist nothing at all. This is ensured by the following line:
```c#
await transaction.Add(basket).Add(order).CommitAsync();
```