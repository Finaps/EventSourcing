using System;
using System.Collections.Generic;
using System.Linq;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Domain.Baskets
{
    public class Basket : Aggregate
    {
        public List<Item> Items = new();
        public bool CheckedOut;
        
        protected override void Apply<TEvent>(TEvent e)
        {
            if (CheckedOut)
                throw new InvalidOperationException("Cannot apply new events to basket that is checked out");
            
            switch (e)
            {
                case BasketCreatedEvent:
                    break;
                case ProductAddedToBasketEvent addedToBasketEvent:
                    var existingItem = Items.SingleOrDefault(x => x.ProductId == addedToBasketEvent.ProductId);
                    if (existingItem != null)
                        existingItem.Quantity += addedToBasketEvent.Quantity;
                    else 
                        Items.Add(new Item(addedToBasketEvent.ProductId, addedToBasketEvent.Quantity));
                    break;
                case ProductRemovedFromBasketEvent removedFromBasketEvent:
                    var itemToRemove = Items.SingleOrDefault(x => x.ProductId == removedFromBasketEvent.ProductId);
                    if (itemToRemove == null)
                        break;
                    itemToRemove.Quantity -= removedFromBasketEvent.Quantity;
                    if (itemToRemove.Quantity <= 0)
                        Items.Remove(itemToRemove);
                    break;
                case BasketCheckedOutEvent:
                    CheckedOut = true;
                    break;
            }
        }

        public void Create()
        {
            Add(new BasketCreatedEvent());
        }
    }
}