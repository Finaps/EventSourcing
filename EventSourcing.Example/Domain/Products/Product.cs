using System.Linq;
using Finaps.EventSourcing.Example.Domain.Shared;

namespace Finaps.EventSourcing.Example.Domain.Products;

public class Product : Aggregate<Product>
{
    public string? Name { get; private set; }
    public int Quantity { get; private set; }
    public List<Reservation> Reservations { get; private set; } = new();

    protected override void Apply(Event<Product> e)
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
                RemoveReservation(soldEvent.BasketId, soldEvent.Quantity);
                break;
            case ProductReservedEvent reservedEvent:
                // The following check makes the entire process of rehydrating this aggregate impure. However the
                // tradeoff is probably worth it since we can ignore old, non-relevant product reservations which is
                // easier on the memory
                if(DateTimeOffset.UtcNow - reservedEvent.Timestamp <= reservedEvent.HeldFor)
                    Reservations.Add(new Reservation(reservedEvent.Quantity, reservedEvent.BasketId));
                break;
            case ReservationRemovedEvent reservationRemovedEvent:
                RemoveReservation(reservationRemovedEvent.BasketId, reservationRemovedEvent.Quantity);
                break;
        }
    }

    protected override void Apply(Snapshot<Product> s)
    {
        switch (s)
        {
            case ProductSnapshot snapshot:
                Name = snapshot.Name;
                Quantity = snapshot.Quantity;
                Reservations = snapshot.Reservations.ToList();
                break;
        }
    }

    public void Create(string name, int initialQuantity)
    {
        if (initialQuantity < 0) throw new ArgumentException("Quantity should not be negative", nameof(initialQuantity));
        Apply(new ProductCreatedEvent(name, initialQuantity));
    }
    public void AddStock(int quantity)
    {
        ValidateQuantity(quantity);
        Apply(new ProductStockAddedEvent(quantity));
    }
    public bool PurchaseProduct(Guid basketId, int quantity)
    {
        ValidateQuantity(quantity);
        if (!CheckAvailabilityForBasket(basketId, quantity)) return false;
        
        Apply(new ProductSoldEvent(quantity, basketId));
        return true;
    }
    public void ReserveProduct(Guid basketId, int quantity)
    {
        ValidateQuantity(quantity);
        if (CheckAvailability(quantity))
            Apply(new ProductReservedEvent(quantity, basketId, Constants.ProductReservationExpires));
    }
    public void RemoveProductReservation(Guid basketId, int quantity)
    {
        ValidateQuantity(quantity);
        if(Reservations.Any(x => x.BasketId == basketId))
            Apply(new ReservationRemovedEvent(quantity, basketId));
    }

    // The available quantity is the total stock minus the amount that is being reserved
    private bool CheckAvailability(int quantity) => 
        Quantity - Reservations.Select(x => x.Quantity).Sum() >= quantity;
    private bool CheckAvailabilityForBasket(Guid basketId, int quantity)
    {
        // Check if total stock is sufficient
        if (Quantity < quantity)
            return false;
        // Check for existing reservation for this basket
        var reserved = Reservations.FirstOrDefault(x => x.BasketId == basketId);
        // If not check if the stock minus all reserved products are sufficient
        if (reserved == null)
            return CheckAvailability(quantity);
        // Else check if the reserved amount is sufficient
        if (reserved.Quantity >= quantity)
            return true;
        // Else check if the non-reserved quantity + the quantity reserved for this basket is sufficient
        return Quantity - Reservations
                   .Where(x => x.BasketId != basketId)
                   .Select(x => x.Quantity)
                   .Sum() + reserved.Quantity
               >= quantity;
    }

    private void RemoveReservation(Guid basketId, int quantity)
    {
        var reservation = Reservations.FirstOrDefault(x => x.BasketId == basketId);
        if(reservation == null)
            return;
                    
        Reservations.Remove(reservation);
        if (quantity < reservation.Quantity)
        {
            Reservations.Add(reservation with
            {
                Quantity = reservation.Quantity - quantity
            });
        }
    }
    private void ValidateQuantity(int quantity)
    {
        if (quantity < 1) throw new ArgumentException("Quantity should be greater than 0");
    }
}