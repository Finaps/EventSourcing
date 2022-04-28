using System.Linq;
using Finaps.EventSourcing.Core;
using Finaps.EventSourcing.Example.Domain.Shared;

namespace Finaps.EventSourcing.Example.Domain.Baskets;

public class Basket : Aggregate<Basket>
{
    public List<Item> Items { get; } = new();
    public bool CheckedOut;
    public DateTimeOffset BasketCreated { get; private set; }
    public DateTimeOffset BasketExpires { get; private set; }

    protected override void Apply(Event<Basket> e)
    {
        if (e is BasketCreatedEvent created)
        {
            BasketCreated = created.Timestamp;
            BasketExpires = BasketCreated + created.ExpirationTime;
            return;
        }

        if (CheckedOut)
            throw new InvalidOperationException("Cannot apply new events to basket that is checked out");
        
        // This check makes sure that we cannot add changes to a basket that is expired
        if (BasketExpires < e.Timestamp)
            throw new InvalidOperationException("Cannot apply new events to basket that is expired");
        
        switch (e)
        {
            case ProductAddedToBasketEvent addedToBasketEvent:
                // Check for existing items in the basket of the product that is being added
                var existingItem = Items.SingleOrDefault(x => x.ProductId == addedToBasketEvent.ProductId);
                // If exists, then replace the item with updated quantity
                if (existingItem != null)
                {
                    Items.Add(new Item(existingItem.ProductId, existingItem.Quantity + addedToBasketEvent.Quantity));
                    Items.Remove(existingItem);
                }
                // Else create a new item with the updated quantity
                else 
                    Items.Add(new Item(addedToBasketEvent.ProductId, addedToBasketEvent.Quantity));
                break;
            case ProductRemovedFromBasketEvent removedFromBasketEvent:
                // Find item in basket to remove
                var itemToRemove = Items.SingleOrDefault(x => x.ProductId == removedFromBasketEvent.ProductId);
                if (itemToRemove == null)
                    break;
                // If exists, create new item with update quantity
                var updated = new Item(itemToRemove.ProductId, itemToRemove.Quantity - removedFromBasketEvent.Quantity);
                // Remove old item
                Items.Remove(itemToRemove);
                // Only add updated item if quantity is greater than 0
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
        Apply(new BasketCreatedEvent(Constants.BasketExpires));
    }
    public void AddProduct(int quantity, Guid productId)
    {
        ValidateQuantity(quantity);
        Apply(new ProductAddedToBasketEvent(quantity, productId));
    }
    public void RemoveProduct(int quantity, Guid productId)
    {   
        ValidateQuantity(quantity);
        if(Items.SingleOrDefault(x => x.ProductId == productId) != null)
            Apply(new ProductRemovedFromBasketEvent(quantity, productId));
    }
    public void CheckoutBasket()
    {
        if (Items.Count == 0 || Items.Sum(x => x.Quantity) == 0)
            throw new InvalidOperationException(
                $"Cannot check out basket with id {Id}: Basket does not contain any items");
        Apply(new BasketCheckedOutEvent());
    }
    
    
    private void ValidateQuantity(int quantity)
    {
        if (quantity < 1) throw new ArgumentException("Quantity should be greater than 0");
    }
}