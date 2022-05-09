namespace Finaps.EventSourcing.Example.Domain.Products;

public class ProductSnapshotFactory : SnapshotFactory<Product, ProductSnapshot>
{
  public override long SnapshotInterval => 10; // For easier testing the snapshotting mechanism we set it to a relatively low number
  protected override ProductSnapshot CreateSnapshot(Product aggregate) => 
    new() {Name = aggregate.Name, Quantity = aggregate.Quantity, Reservations = aggregate.Reservations};
}