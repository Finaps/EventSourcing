using System;
using System.Collections.Generic;
using System.Linq;
using EventSourcing.Core;
using EventSourcing.Example.Domain.Shared;

namespace EventSourcing.Example.Domain.Aggregates.Baskets;

public class Basket : Aggregate
{
    public List<Item> Items = new();
    public bool CheckedOut;
    public bool Expired => BasketExpires <= DateTimeOffset.Now;
    public DateTimeOffset BasketCreated { get; set; }
    public DateTimeOffset BasketExpires => BasketCreated + Constants.BasketExpires;
        
    protected override void Apply<TEvent>(TEvent e)
    {
        if (CheckedOut)
            throw new InvalidOperationException("Cannot apply new events to basket that is checked out");
        
        if (Expired)
            throw new InvalidOperationException("Cannot apply new events to basket that is expired");
        
        switch (e)
        {
            case BasketCreatedEvent:
                BasketCreated = e.Timestamp;
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
        Add(new BasketCheckedOutEvent());
    }
}