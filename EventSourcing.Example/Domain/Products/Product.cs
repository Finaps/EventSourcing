using System;
using System.Collections.Generic;
using EventSourcing.Core;

namespace EventSourcing.Example.Domain.Products
{
    public class Product : Aggregate
    {
        public string Name { get; private set; }
        public int Quantity { get; private set; }
        public List<Reservation> Reservations { get; } = new();
        
        protected override void Apply<TEvent>(TEvent e)
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
            }
        }
    }
    
    
    
    public record Reservation(int Quantity, Guid BasketId);
}