using System;
using System.Collections.Generic;
using System.Linq;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Domain.Products;

public record Product : Aggregate
{
    public string Name { get; private set; }
    public int Quantity { get; private set; }
    public List<Reservation> Reservations { get; private set; } = new();
    public override long SnapshotInterval => 10;

    protected override void Apply(Event e)
    {
        switch (e)
        {
            case ProductCreatedEvent createdEvent:
                Name = createdEvent.Name;
                Quantity += createdEvent.Quantity;
                break;
            case ProductStockAddedEvent addedEvent:
                Quantity += addedEvent.Quantity;
                break;
            case ProductSoldEvent soldEvent:
                Quantity -= soldEvent.Quantity;
                break;
            case ProductReservedEvent reservedEvent:
                if(DateTimeOffset.Now - reservedEvent.Timestamp <= reservedEvent.HeldFor)
                    Reservations.Add(new Reservation(reservedEvent.Quantity, reservedEvent.BasketId));
                break;
            case ReservationRemovedEvent reservationRemovedEvent:
                var reservation = Reservations.FirstOrDefault(x => x.BasketId == reservationRemovedEvent.BasketId);
                if(reservation == null)
                    break;
                    
                Reservations.Remove(reservation);
                if (reservationRemovedEvent.Quantity < reservation.Quantity)
                {
                    Reservations.Add(reservation with
                    {
                        Quantity = reservation.Quantity - reservationRemovedEvent.Quantity
                    });
                }
                break;
        }
    }

    protected override void ApplySnapshot(Snapshot s)
    {
        if (s is not ProductSnapshot productSnapshot)
            throw new ArgumentException($"Expected {nameof(ProductSnapshot)}");
        
        Name = productSnapshot.Name;
        Quantity = productSnapshot.Quantity;
        Reservations = productSnapshot.Reservations;
    }
    public void Create(string name, int initialQuantity)
    {
        Add(new ProductCreatedEvent(name, initialQuantity));
    }
    public void AddStock(int quantity)
    {
        Add(new ProductStockAddedEvent(quantity));
    }
    public void PurchaseProduct(Guid basketId, int quantity)
    {
        if (CheckAvailabilityForBasket(basketId, quantity))
            Add(new ProductSoldEvent(quantity, basketId));
        else Add(new InsufficientStockEvent(basketId));
    }
    public void ReserveProduct(Guid basketId, int quantity)
    {
        if (CheckAvailability(quantity))
            Add(new ProductReservedEvent(quantity, basketId, Constants.ProductReservationExpires));
    }
    public void RemoveReservation(Guid basketId, int quantity)
    {
        Add(new ReservationRemovedEvent(quantity, basketId));
    }


        
    private bool CheckAvailability(int quantity) => 
        Quantity - Reservations.Select(x => x.Quantity).Sum() >= quantity;
    private bool CheckAvailabilityForBasket(Guid basketId, int quantity) => 
        Quantity - Reservations.Where(x => x.BasketId != basketId).Select(x => x.Quantity).Sum() >= quantity;

    protected override Snapshot CreateSnapshot()
    {
        return new ProductSnapshot(Name, Quantity, Reservations);
    }
}
    
    
    
public record Reservation(int Quantity, Guid BasketId);