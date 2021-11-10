using System;

namespace EventSourcing.Example.Domain.Shared
{
    public class Item
    {
        public Guid ProductId { get; }
        public int Quantity { get; set; }
        public Item(Guid productId, int quantity)
        {
            ProductId = productId;
            Quantity = quantity;
        }
    }
}