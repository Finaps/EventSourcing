using System;
using System.Collections.Generic;
using System.Linq;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Domain.Baskets;

public class Basket : Aggregate
{
    public List<Item> Items { get; } = new();
    public bool CheckedOut;
    public DateTimeOffset BasketCreated { get; private set; }
    public DateTimeOffset BasketExpires { get; private set; }

    protected override void Apply(Event e)
    {
        if (e is BasketCreatedEvent created)
        {
            BasketCreated = created.Timestamp;
            BasketExpires = BasketCreated + created.ExpirationTime;
            return;
        }

        if (CheckedOut)
            throw new InvalidOperationException("Cannot apply new events to basket that is checked out");
        
        if (BasketExpires < e.Timestamp)
            throw new InvalidOperationException("Cannot apply new events to basket that is expired");
        
        switch (e)
        {
            case ProductAddedToBasketEvent addedToBasketEvent:
                var existingItem = Items.SingleOrDefault(x => x.ProductId == addedToBasketEvent.ProductId);
                if (existingItem != null)
                {
                    Items.Add(new Item(existingItem.ProductId, existingItem.Quantity + addedToBasketEvent.Quantity));
                    Items.Remove(existingItem);
                }
                else 
                    Items.Add(new Item(addedToBasketEvent.ProductId, addedToBasketEvent.Quantity));
                break;
            case ProductRemovedFromBasketEvent removedFromBasketEvent:
                var itemToRemove = Items.SingleOrDefault(x => x.ProductId == removedFromBasketEvent.ProductId);
                if (itemToRemove == null)
                    break;
                var updated = new Item(itemToRemove.ProductId, itemToRemove.Quantity - removedFromBasketEvent.Quantity);
                Items.Remove(itemToRemove);
                if (itemToRemove.Quantity > 0)
                    Items.Add(updated);
                break;
            case BasketCheckedOutEvent:
                CheckedOut = true;
                break;
        }
    }

    public void Create()
    {
        Add(new BasketCreatedEvent(Constants.BasketExpires));
    }
    public void AddProduct(int quantity, Guid productId)
    {
        Add(new ProductAddedToBasketEvent(quantity, productId));
    }
    public void RemoveProduct(int quantity, Guid productId)
    {   
        if(Items.SingleOrDefault(x => x.ProductId == productId) != null)
            Add(new ProductRemovedFromBasketEvent(quantity, productId));
    }
    public void CheckoutBasket()
    {
        if (Items.Count == 0)
            throw new InvalidOperationException(
                $"Cannot check out basket with id {Id}: Basket does not contain any items");
        Add(new BasketCheckedOutEvent());
    }
}